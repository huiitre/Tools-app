using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

namespace Tools.Api.IntegrationTests.Fakes;

// Remplace le provider du jeu utilisé par les tests de poll : aucun appel réseau, statut piloté
// par le test. Il n'implémente pas IGameServerDashboard, donc ce jeu n'a pas de dashboard.
public sealed class FakeGameServerProvider : IGameServerProvider
{
    public string GameCode { get; set; } = "RUST";

    public GameServerStatus Status { get; set; } = new(true, 3, 50);
    public bool ShouldThrow { get; set; }

    public Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Échec de poll simulé.");
        }

        return Task.FromResult(Status);
    }
}
