using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

namespace Tools.Api.IntegrationTests.Fakes;

// Pendant de FakeGameServerProvider pour un jeu qui, lui, sait alimenter un dashboard. Les deux
// coexistent afin que les tests couvrent aussi le jeu qui n'en a pas.
public sealed class FakeGameServerDashboardProvider : IGameServerProvider, IGameServerDashboard
{
    public string GameCode => "ARK_SA";

    public GameServerStatus Status { get; set; } = new(true, 1, 12);

    public Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        return Task.FromResult(Status);
    }

    public Task<GameServerDetailsView> FetchDetailsAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        // Reproduit le contrat réel : l'identité vient de la cible, le reste est laissé vide par
        // un jeu qui ne sait pas le fournir.
        return Task.FromResult(new GameServerDetailsView(
            target.ServerName,
            target.GameName,
            target.PictureUrl,
            Version: null,
            Description: null,
            WorldId: null,
            Settings: null,
            Actions: []));
    }

    public Task<GameServerLiveView> FetchLiveAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GameServerLiveView(
            1, 12, null, null, null, null, null, null,
            [new GameServerLivePlayer("Huiitre", "abc", null, null, null, null, null, null, null, null, null, null, null)],
            [],
            [],
            []));
    }
}
