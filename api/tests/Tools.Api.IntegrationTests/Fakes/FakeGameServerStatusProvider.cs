using Tools.Api.Modules.Core.GameServers.Application.Dto;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class FakeGameServerStatusProvider : IGameServerStatusProvider
{
    public string ProtocolType => "STEAM_A2S";

    public GameServerStatus Status { get; set; } = new(true, 3, 50);
    public bool ShouldThrow { get; set; }

    public Task<GameServerStatus> FetchAsync(GameServerPollTarget gameServer, CancellationToken cancellationToken)
    {
        if (ShouldThrow)
        {
            throw new InvalidOperationException("Échec de poll simulé.");
        }

        return Task.FromResult(Status);
    }
}
