using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.Security.Application.Services;
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
        return Execute(slug, (provider, target, token) => provider.FetchDetailsAsync(target, token), cancellationToken);
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
