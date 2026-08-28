using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.GameServers;

public sealed class GameServerDashboardTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public GameServerDashboardTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Servers.Clear();
        // ARK_SA a un provider enregistré, RUST n'en a pas : les deux cas du dashboard.
        ManifestProvider.Manifests =
        [
            new GameServerSyncDto(
                "ark-survival-ascended", "ARK_SA", "SOURCE_RCON", "Huiitre ASA", 2399830, null,
                "172.19.0.7", 27020, "games.huiitre.fr", 7777, JsonDocument.Parse("{}").RootElement.Clone()),
            new GameServerSyncDto(
                "rust", "RUST", "STEAM_A2S", "Huiitre Rust", 252490, null,
                "172.19.0.7", 28017, "games.huiitre.fr", 28015, JsonDocument.Parse("{}").RootElement.Clone())
        ];
    }

    private InMemoryGameServerRepository Servers => factory.Services.GetRequiredService<InMemoryGameServerRepository>();
    private FakeGameServersManifestProvider ManifestProvider => factory.Services.GetRequiredService<FakeGameServersManifestProvider>();

    private async Task SyncAsync()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiAttribute.HeaderName, ApiWebApplicationFactory.TestInternalToken);
        using var response = await client.PostAsync("/internal/gameservers/sync", null);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Dashboard_requires_authentication()
    {
        await SyncAsync();

        using var anonymous = factory.CreateClient();
        using var live = await anonymous.GetAsync("/gameservers/ark-survival-ascended/live");
        Assert.Equal(HttpStatusCode.Unauthorized, live.StatusCode);
    }

    [Fact]
    public async Task Unknown_slug_is_not_found()
    {
        await SyncAsync();

        using var client = factory.CreateClientWithRole("READ_ONLY");
        using var response = await client.GetAsync("/gameservers/inconnu/live");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Game_without_provider_has_no_dashboard()
    {
        await SyncAsync();

        using var client = factory.CreateClientWithRole("READ_ONLY");
        using var response = await client.GetAsync("/gameservers/rust/live");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Listing_says_which_servers_have_a_dashboard()
    {
        await SyncAsync();

        using var client = factory.CreateClientWithRole("READ_ONLY");
        var gameServers = await client.GetFromJsonAsync<IReadOnlyList<GameServerDashboardView>>("/gameservers");
        Assert.NotNull(gameServers);

        Assert.True(gameServers.Single(gameServer => gameServer.Slug == "ark-survival-ascended").HasDashboard);
        Assert.False(gameServers.Single(gameServer => gameServer.Slug == "rust").HasDashboard);
    }

    [Fact]
    public async Task Details_are_served_from_the_database_when_the_game_cannot_provide_them()
    {
        await SyncAsync();

        using var client = factory.CreateClientWithRole("READ_ONLY");
        using var response = await client.GetAsync("/gameservers/ark-survival-ascended/details");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<GameServerDetailsView>();
        Assert.NotNull(details);
        Assert.Equal("Huiitre ASA", details.ServerName);
        // Ark n'expose aucune de ces informations par RCON : le front affichera « indisponible ».
        Assert.Null(details.Version);
        Assert.Null(details.Description);
        Assert.Null(details.WorldId);
    }
}
