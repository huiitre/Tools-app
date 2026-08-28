using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Listing;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

// Le rôle READ_ONLY par défaut de SecuredUseCase suffit : un appel authentifié ne lit qu'un
// snapshot persistant, sans déclencher aucune connexion réseau vers les serveurs de jeu.
public sealed class GetGameServersUseCase(
    UseCaseAuthorizer authorizer,
    IGameServerDashboardRepository gameServerDashboardRepository,
    IEnumerable<IGameServerProvider> providers) : SecuredUseCase(authorizer)
{
    // Les jeux dont le provider sait alimenter un dashboard. Rien à déclarer à la main : ajouter
    // IGameServerDashboard à un provider suffit à faire apparaître le bouton côté front.
    private readonly HashSet<string> gameCodesWithDashboard = providers
        .OfType<IGameServerDashboard>()
        .Select(provider => ((IGameServerProvider)provider).GameCode)
        .ToHashSet(StringComparer.Ordinal);

    public async Task<IReadOnlyList<GameServerDashboardView>> Execute()
    {
        var gameServers = await gameServerDashboardRepository.FindVisibleForDashboardAsync();
        return gameServers
            .Select(gameServer => new GameServerDashboardView(
                gameServer.Slug,
                gameServer.GameCode,
                gameCodesWithDashboard.Contains(gameServer.GameCode),
                gameServer.GameName,
                gameServer.ServerName,
                gameServer.PictureUrl,
                gameServer.Online,
                gameServer.NumPlayers,
                gameServer.MaxPlayers,
                gameServer.CheckedAt,
                gameServer.ClientHost,
                gameServer.ClientPort))
            .ToList();
    }
}
