using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// Interroge un serveur en direct, contrairement à GetGameServersUseCase qui ne lit qu'un
// snapshot. Le provider est choisi par gameCode : un jeu sans provider n'a pas de dashboard.
public sealed class GetGameServerDashboardUseCase(
    UseCaseAuthorizer authorizer,
    IGameServerTargetRepository gameServerTargetRepository,
    IGameServerRawCommandHistoryRepository rawCommandHistoryRepository,
    PollGameServersUseCase pollGameServersUseCase,
    IEnumerable<IGameServerProvider> providers) : SecuredUseCase(authorizer)
{
    // Borne les appels réseau : un serveur qui accepte la connexion sans jamais répondre
    // laisserait sinon la requête HTTP ouverte indéfiniment.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    // Un historique d'admin consulté à l'ouverture de la console, pas un flux à paginer.
    private const int HistoryLimit = 100;

    // Seuls les jeux qui implémentent IGameServerDashboard ont un dashboard : les autres ne sont
    // même pas indexés ici.
    private readonly IReadOnlyDictionary<string, IGameServerDashboard> dashboardsByGameCode = providers
        .OfType<IGameServerDashboard>()
        .ToDictionary(provider => ((IGameServerProvider)provider).GameCode, StringComparer.Ordinal);

    // Seuls les jeux qui implémentent IGameServerRawCommand acceptent une commande libre : les
    // autres n'ont ni entrée ici ni bloc console côté front.
    private readonly IReadOnlyDictionary<string, IGameServerRawCommand> rawCommandByGameCode = providers
        .OfType<IGameServerRawCommand>()
        .ToDictionary(provider => ((IGameServerProvider)provider).GameCode, StringComparer.Ordinal);

    public async Task<GameServerDetailsView> ExecuteDetails(string slug, CancellationToken cancellationToken)
    {
        var target = await gameServerTargetRepository.FindBySlugAsync(slug)
            ?? throw AppException.NotFound("GAME_SERVER_NOT_FOUND", $"Aucun serveur de jeu visible pour le slug « {slug} ».");

        if (!dashboardsByGameCode.TryGetValue(target.GameCode, out var provider))
        {
            throw AppException.NotFound(
                "GAME_SERVER_DASHBOARD_UNSUPPORTED",
                $"Aucun dashboard n'est disponible pour le jeu « {target.GameCode} ».");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);

        GameServerDetailsView details;
        try
        {
            details = await provider.FetchDetailsAsync(target, timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw AppException.Unavailable(
                "GAME_SERVER_UNREACHABLE",
                $"Le serveur « {target.Slug} » n'a pas répondu dans le délai imparti.");
        }

        // Les actions sont ajoutées ici et non par le provider : lui ne connaît pas l'appelant.
        // Seules celles que son rôle autorise lui sont annoncées.
        var actions = provider is IGameServerActions actionable
            ? actionable.Actions.Where(action => CurrentUser.Role?.HasAtLeast(action.Role) == true).ToList()
            : [];
        // La commande libre équivaut à un accès admin total au jeu : réservée au rôle le plus
        // élevé, indépendamment de ce que le jeu déclare.
        var supportsRawCommand = provider is IGameServerRawCommand
                                  && CurrentUser.Role?.HasAtLeast(RoleCode.Admin) == true;

        return details with { Actions = actions, SupportsRawCommand = supportsRawCommand };
    }

    public async Task<string?> ExecuteRawCommand(string slug, string command, CancellationToken cancellationToken)
    {
        var target = await gameServerTargetRepository.FindBySlugAsync(slug)
            ?? throw AppException.NotFound("GAME_SERVER_NOT_FOUND", $"Aucun serveur de jeu visible pour le slug « {slug} ».");

        if (!rawCommandByGameCode.TryGetValue(target.GameCode, out var rawCommand))
        {
            throw AppException.NotFound(
                "GAME_SERVER_RAW_COMMAND_UNSUPPORTED",
                $"Le jeu « {target.GameCode} » n'accepte pas de commande libre.");
        }

        // Toujours ADMIN, quel que soit le jeu : une commande libre n'a pas de rôle propre comme
        // une action déclarée, elle équivaut à la plus dangereuse d'entre elles.
        authorizer.EnsureAtLeast(RoleCode.Admin);

        if (string.IsNullOrWhiteSpace(command))
        {
            throw AppException.Validation("GAME_SERVER_RAW_COMMAND_EMPTY", "La commande ne peut pas être vide.");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);

        string? answer;
        try
        {
            answer = await rawCommand.ExecuteRawCommandAsync(target, command.Trim(), timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw AppException.Unavailable(
                "GAME_SERVER_UNREACHABLE",
                $"Le serveur « {target.Slug} » n'a pas répondu dans le délai imparti.");
        }

        // Seul un aller-retour réussi est audité : un échec de connexion n'a jamais atteint le
        // jeu, il n'y a rien à tracer (déjà dans les logs techniques).
        await rawCommandHistoryRepository.InsertAsync(target.Id, CurrentUser.UserId, command.Trim(), answer);

        // Une commande libre peut changer n'importe quoi (op, kick, gamerule…) : on ne sait pas
        // quoi, donc on rafraîchit toujours, plutôt que d'attendre jusqu'à 10s le prochain tick.
        await pollGameServersUseCase.RefreshOneAsync(target, cancellationToken);

        return answer;
    }

    public async Task<IReadOnlyList<GameServerRawCommandHistoryEntry>> ExecuteRawCommandHistory(
        string slug,
        CancellationToken cancellationToken)
    {
        var target = await gameServerTargetRepository.FindBySlugAsync(slug)
            ?? throw AppException.NotFound("GAME_SERVER_NOT_FOUND", $"Aucun serveur de jeu visible pour le slug « {slug} ».");

        if (!rawCommandByGameCode.ContainsKey(target.GameCode))
        {
            throw AppException.NotFound(
                "GAME_SERVER_RAW_COMMAND_UNSUPPORTED",
                $"Le jeu « {target.GameCode} » n'accepte pas de commande libre.");
        }

        // Même exigence que l'exécution : l'historique révèle ce que d'autres admins ont tapé.
        authorizer.EnsureAtLeast(RoleCode.Admin);

        return await rawCommandHistoryRepository.FindRecentAsync(target.Id, HistoryLimit);
    }

    public async Task ExecuteAction(
        string slug,
        string actionCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var target = await gameServerTargetRepository.FindBySlugAsync(slug)
            ?? throw AppException.NotFound("GAME_SERVER_NOT_FOUND", $"Aucun serveur de jeu visible pour le slug « {slug} ».");

        if (!dashboardsByGameCode.TryGetValue(target.GameCode, out var dashboard)
            || dashboard is not IGameServerActions actionable)
        {
            throw AppException.NotFound(
                "GAME_SERVER_ACTIONS_UNSUPPORTED",
                $"Le jeu « {target.GameCode} » n'accepte aucune commande d'administration.");
        }

        var action = actionable.Actions.FirstOrDefault(candidate => candidate.Code == actionCode)
            ?? throw AppException.NotFound(
                "GAME_SERVER_ACTION_UNKNOWN",
                $"L'action « {actionCode} » n'existe pas pour ce serveur.");

        // Le rôle exigé dépend de l'action : le contrôle du constructeur ne suffit pas.
        authorizer.EnsureAtLeast(action.Role);

        var missing = action.Parameters
            .Where(parameter => parameter.Required
                                && (!parameters.TryGetValue(parameter.Name, out var value)
                                    || string.IsNullOrWhiteSpace(value)))
            .Select(parameter => parameter.Name)
            .ToList();

        if (missing.Count > 0)
        {
            throw AppException.Validation(
                "GAME_SERVER_ACTION_PARAMETERS_MISSING",
                $"Paramètres obligatoires manquants : {string.Join(", ", missing)}.");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);

        try
        {
            await actionable.ExecuteAsync(target, actionCode, parameters, timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw AppException.Unavailable(
                "GAME_SERVER_UNREACHABLE",
                $"Le serveur « {target.Slug} » n'a pas répondu dans le délai imparti.");
        }

        // Sans ça, l'effet d'un kick/ban/annonce ne serait visible qu'au prochain tick du
        // scheduler partagé (jusqu'à 10s) — trop lent pour une action que l'admin vient de lancer.
        await pollGameServersUseCase.RefreshOneAsync(target, cancellationToken);
    }

}
