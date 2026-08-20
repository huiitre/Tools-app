using Tools.Api.Modules.Core.GameServers.Application.Dto;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class FakeGameServersManifestProvider : IGameServersManifestProvider
{
    public IReadOnlyList<GameServerSyncDto> Manifests { get; set; } = [];

    public Task<IReadOnlyList<GameServerSyncDto>> FetchAsync() => Task.FromResult(Manifests);
}
