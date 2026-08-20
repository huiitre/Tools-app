using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.GameServers.Application;
using Tools.Api.Modules.Core.GameServers.Application.Dto;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.GameServers;

public sealed class GameServersPollingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public GameServersPollingTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Servers.Clear();
        Provider.Status = new GameServerStatus(true, 3, 50);
        Provider.ShouldThrow = false;
    }

    private InMemoryGameServerRepository Servers => factory.Services.GetRequiredService<InMemoryGameServerRepository>();
    private FakeGameServerStatusProvider Provider => factory.Services.GetRequiredService<FakeGameServerStatusProvider>();

    [Fact]
    public async Task Polling_uses_the_protocol_provider_and_writes_only_its_status()
    {
        await Servers.UpsertAsync(new GameServerSyncEntry(
            "rust", "RUST", "STEAM_A2S", "Rust", 252490, "172.19.0.7", 28017, "{}",
            "Rust", null, false, true, "games.huiitre.fr", 28015));

        await using var scope = factory.Services.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<PollGameServersUseCase>();
        await useCase.Execute(CancellationToken.None);

        var status = Assert.Single(Servers.Statuses).Value;
        Assert.True(status.Online);
        Assert.Equal(3, status.NumPlayers);
        Assert.Equal(50, status.MaxPlayers);
    }

    [Fact]
    public async Task Polling_an_unknown_protocol_marks_that_server_offline_without_failing_the_pass()
    {
        await Servers.UpsertAsync(new GameServerSyncEntry(
            "unknown", "UNKNOWN", "UNSUPPORTED", "Unknown", null, "172.19.0.8", 1, "{}",
            null, null, false, true, "games.huiitre.fr", 1));

        await using var scope = factory.Services.CreateAsyncScope();
        var useCase = scope.ServiceProvider.GetRequiredService<PollGameServersUseCase>();
        await useCase.Execute(CancellationToken.None);

        var status = Assert.Single(Servers.Statuses).Value;
        Assert.False(status.Online);
        Assert.Null(status.NumPlayers);
        Assert.Null(status.MaxPlayers);
    }
}
