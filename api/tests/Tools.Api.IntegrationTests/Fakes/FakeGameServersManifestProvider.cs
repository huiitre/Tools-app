using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class FakeGameServersManifestProvider : IGameServersManifestProvider
{
    public IReadOnlyList<GameServerSyncDto> Manifests { get; set; } = [];

    public Task<IReadOnlyList<GameServerSyncDto>> FetchAsync() => Task.FromResult(Manifests);
}
