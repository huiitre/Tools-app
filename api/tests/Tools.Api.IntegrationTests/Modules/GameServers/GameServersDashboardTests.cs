using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Common.Api.Internal;
using Tools.Api.Modules.GameServers.Application.Dto;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.GameServers;

public sealed class GameServersDashboardTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public GameServersDashboardTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Servers.Clear();
        ManifestProvider.Manifests = [new GameServerSyncDto(
            "rust", "RUST", "STEAM_A2S", "Huiitre Rust Server PvE", 252490, null,
            "172.19.0.7", 28017, "games.huiitre.fr", 28015, JsonDocument.Parse("{}").RootElement.Clone())];
        Steam.Set(252490, "Rust", "https://steam.example/rust-header.jpg");
    }

    private InMemoryGameServerRepository Servers => factory.Services.GetRequiredService<InMemoryGameServerRepository>();
    private FakeGameServersManifestProvider ManifestProvider => factory.Services.GetRequiredService<FakeGameServersManifestProvider>();
    private FakeSteamAppDetailsProvider Steam => factory.Services.GetRequiredService<FakeSteamAppDetailsProvider>();

    [Fact]
    public async Task Dashboard_requires_read_only_and_never_returns_connection_configuration()
    {
        using var syncClient = factory.CreateClient();
        syncClient.DefaultRequestHeaders.Add(InternalApiAttribute.HeaderName, ApiWebApplicationFactory.TestInternalToken);
        using var sync = await syncClient.PostAsync("/internal/gameservers/sync", null);
        sync.EnsureSuccessStatusCode();

        using var anonymous = factory.CreateClient();
        using var rejected = await anonymous.GetAsync("/gameservers");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);

        using var client = factory.CreateClientWithRoles("READ_ONLY");
        using var response = await client.GetAsync("/gameservers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var server = Assert.Single(payload.EnumerateArray());
        Assert.Equal("Rust", server.GetProperty("gameName").GetString());
        Assert.Equal("Huiitre Rust Server PvE", server.GetProperty("serverName").GetString());
        Assert.False(server.TryGetProperty("host", out _));
        Assert.False(server.TryGetProperty("port", out _));
        Assert.False(server.TryGetProperty("protocolConfig", out _));
    }
}
