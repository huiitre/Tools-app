using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Common.Api.Internal;
using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.GameServers;

public sealed class GameServersSyncTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public GameServersSyncTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Servers.Clear();
        Steam.Set(252490, "Rust", "https://steam.example/rust-header.jpg");
        Steam.Set(1623730, "Palworld", "https://steam.example/palworld-header.jpg");
    }

    private InMemoryGameServerRepository Servers => factory.Services.GetRequiredService<InMemoryGameServerRepository>();
    private FakeSteamAppDetailsProvider Steam => factory.Services.GetRequiredService<FakeSteamAppDetailsProvider>();
    private FakeGameServersManifestProvider ManifestProvider => factory.Services.GetRequiredService<FakeGameServersManifestProvider>();

    private HttpClient ClientWithToken()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiAttribute.HeaderName, ApiWebApplicationFactory.TestInternalToken);
        return client;
    }

    [Fact]
    public async Task Syncing_a_new_manifest_creates_it_and_prefers_the_local_asset_url()
    {
        ManifestProvider.Manifests = [Manifest("rust", 252490, "img/rust.png")];
        using var response = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertReport(response, created: 1, updated: 0, unchanged: 0, deleted: 0);

        var server = Assert.Single(Servers.GameServers);
        Assert.Equal("Rust", server.GameName);
        Assert.Equal("https://assets.tools.huiitre.fr/tools_core/gameservers/img/rust.png", server.PictureUrl);
    }

    [Fact]
    public async Task Syncing_the_same_manifest_does_not_report_a_configuration_update()
    {
        ManifestProvider.Manifests = [Manifest("rust", 252490, "img/rust.png")];
        using var first = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);
        first.EnsureSuccessStatusCode();

        using var second = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);

        await AssertReport(second, created: 0, updated: 0, unchanged: 1, deleted: 0);
    }

    [Fact]
    public async Task Removing_a_local_picture_switches_to_the_steam_image_and_reports_an_update()
    {
        ManifestProvider.Manifests = [Manifest("rust", 252490, "img/rust.png")];
        using var first = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);
        first.EnsureSuccessStatusCode();

        ManifestProvider.Manifests = [Manifest("rust", 252490, pictureFile: null)];
        using var second = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);

        await AssertReport(second, created: 0, updated: 1, unchanged: 0, deleted: 0);
        Assert.Equal("https://steam.example/rust-header.jpg", Assert.Single(Servers.GameServers).PictureUrl);
    }

    [Fact]
    public async Task A_steam_outage_never_erases_previously_synced_metadata()
    {
        ManifestProvider.Manifests = [Manifest("rust", 252490, pictureFile: null)];
        using var first = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);
        first.EnsureSuccessStatusCode();

        Steam.SetUnavailable(252490);
        using var second = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);

        await AssertReport(second, created: 0, updated: 0, unchanged: 1, deleted: 0);
        var server = Assert.Single(Servers.GameServers);
        Assert.Equal("Rust", server.GameName);
        Assert.Equal("https://steam.example/rust-header.jpg", server.PictureUrl);
    }

    [Fact]
    public async Task Syncing_a_scan_without_a_previous_slug_deletes_only_that_server()
    {
        ManifestProvider.Manifests = [Manifest("rust", 252490, null), Manifest("palworld", 1623730, null)];
        using var first = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);
        first.EnsureSuccessStatusCode();

        ManifestProvider.Manifests = [Manifest("palworld", 1623730, null)];
        using var second = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);

        await AssertReport(second, created: 0, updated: 0, unchanged: 1, deleted: 1);
        Assert.Equal("palworld", Assert.Single(Servers.GameServers).Slug);
    }

    [Fact]
    public async Task Invalid_duplicate_slugs_are_rejected_before_any_deletion()
    {
        ManifestProvider.Manifests = [Manifest("rust", 252490, null)];
        using var first = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);
        first.EnsureSuccessStatusCode();

        ManifestProvider.Manifests = [Manifest("palworld", 1623730, null), Manifest("palworld", 1623730, null)];
        using var invalid = await ClientWithToken().PostAsync("/internal/gameservers/sync", null);

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("rust", Assert.Single(Servers.GameServers).Slug);
    }

    private static GameServerSyncDto Manifest(string slug, int steamAppId, string? pictureFile) => new(
        slug,
        slug == "rust" ? "RUST" : "PALWORLD",
        slug == "rust" ? "STEAM_A2S" : "PALWORLD_REST",
        slug == "rust" ? "Huiitre Rust Server PvE" : "Chez huihui",
        steamAppId,
        pictureFile,
        "172.19.0.7",
        slug == "rust" ? 28017 : 8212,
        JsonDocument.Parse("{}").RootElement.Clone());

    private static async Task AssertReport(
        HttpResponseMessage response,
        int created,
        int updated,
        int unchanged,
        int deleted)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created, report.GetProperty("created").GetInt32());
        Assert.Equal(updated, report.GetProperty("updated").GetInt32());
        Assert.Equal(unchanged, report.GetProperty("unchanged").GetInt32());
        Assert.Equal(deleted, report.GetProperty("deleted").GetInt32());
    }
}
