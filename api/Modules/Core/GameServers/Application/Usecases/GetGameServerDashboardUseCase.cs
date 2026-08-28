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
    IEnumerable<IGameServerProvider> providers) : SecuredUseCase(authorizer)
{
    // Borne les appels réseau : un serveur qui accepte la connexion sans jamais répondre
    // laisserait sinon la requête HTTP ouverte indéfiniment.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    // Seuls les jeux qui implémentent IGameServerDashboard ont un dashboard : les autres ne sont
    // même pas indexés ici.
    private readonly IReadOnlyDictionary<string, IGameServerDashboard> dashboardsByGameCode = providers
        .OfType<IGameServerDashboard>()
        .ToDictionary(provider => ((IGameServerProvider)provider).GameCode, StringComparer.Ordinal);

    public Task<GameServerDetailsView> ExecuteDetails(string slug, CancellationToken cancellationToken)
    {
        // Les actions sont ajoutées ici et non par le provider : lui ne connaît pas l'appelant.
        // Seules celles que son rôle autorise lui sont annoncées.
        return Execute(slug, async (provider, target, token) =>
        {
            var details = await provider.FetchDetailsAsync(target, token);
            var actions = provider is IGameServerActions actionable
                ? actionable.Actions.Where(action => CurrentUser.Role?.HasAtLeast(action.Role) == true).ToList()
                : [];

            return details with { Actions = actions };
        }, cancellationToken);
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
    }

    public Task<GameServerLiveView> ExecuteLive(string slug, CancellationToken cancellationToken)
    {
        return Execute(slug, (provider, target, token) => provider.FetchLiveAsync(target, token), cancellationToken);
    }

    private async Task<T> Execute<T>(
        string slug,
        Func<IGameServerDashboard, GameServerTarget, CancellationToken, Task<T>> fetch,
        CancellationToken cancellationToken)
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

        try
        {
            return await fetch(provider, target, timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw AppException.Unavailable(
                "GAME_SERVER_UNREACHABLE",
                $"Le serveur « {target.Slug} » n'a pas répondu dans le délai imparti.");
        }
    }
}
