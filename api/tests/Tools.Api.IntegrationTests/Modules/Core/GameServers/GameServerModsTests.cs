using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.GameServers;

public sealed class GameServerModsTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string Slug = "minecraft-cobblemon";
    private const string Accessories = "https://mods.example/accessories";
    private const string Architectury = "https://mods.example/architectury";

    private readonly ApiWebApplicationFactory factory;

    public GameServerModsTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Servers.Clear();
        Icons.Clear();
        Icons.Set(Accessories, "https://cdn.example/accessories.png");
        Icons.Set(Architectury, "https://cdn.example/architectury.png");
    }

    private InMemoryGameServerRepository Servers => factory.Services.GetRequiredService<InMemoryGameServerRepository>();
    private FakeModIconResolver Icons => factory.Services.GetRequiredService<FakeModIconResolver>();
    private FakeGameServersManifestProvider ManifestProvider => factory.Services.GetRequiredService<FakeGameServersManifestProvider>();

    [Fact]
    public async Task Mods_are_stored_in_order_with_their_icons_and_a_versioned_modpack_url()
    {
        ManifestProvider.Manifests = [Manifest(
            [
                Mod("Accessories", Accessories),
                Mod("SimpleTMs", url: null, icon: "https://cdn.example/manual.png"),
                Mod("Unknown host", "https://elsewhere.example/mod")
            ],
            modpackFile: "modpacks/minecraft-cobblemon.zip")];
        using var sync = await Sync();
        await AssertReport(sync, created: 1, updated: 0, unchanged: 0);

        var payload = await GetMods(Slug);
        Assert.Equal(
            "https://assets.tools.huiitre.fr/tools_core/gameservers/modpacks/minecraft-cobblemon.zip?v=42ecff15",
            payload.GetProperty("modpackUrl").GetString());
        Assert.Equal(350_604_203, payload.GetProperty("modpackSize").GetInt64());

        var mods = payload.GetProperty("mods").EnumerateArray().ToList();
        Assert.Equal(
            new[] { "Accessories", "SimpleTMs", "Unknown host" },
            mods.Select(mod => mod.GetProperty("name").GetString()!));
        Assert.Equal("https://cdn.example/accessories.png", mods[0].GetProperty("iconUrl").GetString());
        Assert.Equal("https://cdn.example/manual.png", mods[1].GetProperty("iconUrl").GetString());
        Assert.Equal(JsonValueKind.Null, mods[2].GetProperty("iconUrl").ValueKind);
        Assert.Equal("Author", Assert.Single(mods[0].GetProperty("authors").EnumerateArray()).GetString());
        Assert.Equal("Accessories.jar", mods[0].GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task A_known_icon_is_never_requested_again_and_an_identical_list_is_unchanged()
    {
        ManifestProvider.Manifests = [Manifest([Mod("Accessories", Accessories)])];
        using var first = await Sync();
        first.EnsureSuccessStatusCode();

        using var second = await Sync();

        await AssertReport(second, created: 0, updated: 0, unchanged: 1);
        Assert.Single(Icons.Requests);
    }

    [Fact]
    public async Task Removed_mods_disappear_new_ones_appear_and_remaining_ones_keep_their_icon()
    {
        ManifestProvider.Manifests = [Manifest([Mod("Accessories", Accessories), Mod("Old mod", "https://mods.example/old")])];
        using var first = await Sync();
        first.EnsureSuccessStatusCode();

        ManifestProvider.Manifests = [Manifest([Mod("Accessories", Accessories), Mod("Architectury", Architectury)])];
        using var second = await Sync();

        await AssertReport(second, created: 0, updated: 1, unchanged: 0);
        var mods = Servers.ModsOf(Slug);
        Assert.Equal(new[] { "Accessories", "Architectury" }, mods.Select(mod => mod.Name));
        Assert.Equal("https://cdn.example/accessories.png", mods[0].IconUrl);
        Assert.Equal("https://cdn.example/architectury.png", mods[1].IconUrl);
        Assert.DoesNotContain(Accessories, Icons.Requests[1]);
    }

    [Fact]
    public async Task A_host_outage_only_leaves_icons_empty_until_the_next_sync()
    {
        Icons.Unavailable = true;
        ManifestProvider.Manifests = [Manifest([Mod("Accessories", Accessories)])];
        using var first = await Sync();

        await AssertReport(first, created: 1, updated: 0, unchanged: 0);
        Assert.Null(Assert.Single(Servers.ModsOf(Slug)).IconUrl);

        Icons.Unavailable = false;
        using var second = await Sync();

        await AssertReport(second, created: 0, updated: 1, unchanged: 0);
        Assert.Equal("https://cdn.example/accessories.png", Assert.Single(Servers.ModsOf(Slug)).IconUrl);
    }

    [Fact]
    public async Task The_widget_knows_which_servers_have_mods_and_the_mods_route_requires_an_account()
    {
        ManifestProvider.Manifests =
        [
            Manifest([Mod("Accessories", Accessories), Mod("Architectury", Architectury)], "modpacks/minecraft-cobblemon.zip"),
            Manifest(mods: null, slug: "rust")
        ];
        using var sync = await Sync();
        sync.EnsureSuccessStatusCode();

        using var client = factory.CreateClientWithRole("READ_ONLY");
        var servers = await client.GetFromJsonAsync<JsonElement>("/gameservers");
        var cobblemon = servers.EnumerateArray().Single(server => server.GetProperty("slug").GetString() == Slug);
        var rust = servers.EnumerateArray().Single(server => server.GetProperty("slug").GetString() == "rust");
        Assert.Equal(2, cobblemon.GetProperty("modCount").GetInt32());
        Assert.True(cobblemon.GetProperty("hasModpack").GetBoolean());
        Assert.Equal(0, rust.GetProperty("modCount").GetInt32());
        Assert.False(rust.GetProperty("hasModpack").GetBoolean());

        var rustMods = await GetMods("rust");
        Assert.Empty(rustMods.GetProperty("mods").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, rustMods.GetProperty("modpackUrl").ValueKind);

        using var unknown = await client.GetAsync("/gameservers/unknown/mods");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        using var anonymous = await factory.CreateClient().GetAsync($"/gameservers/{Slug}/mods");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Theory]
    [InlineData("minecraft-cobblemon.zip")]
    [InlineData("img/minecraft-cobblemon.zip")]
    [InlineData("modpacks/../gameservers.json")]
    public async Task A_modpack_outside_the_modpacks_directory_is_rejected(string modpackFile)
    {
        ManifestProvider.Manifests = [Manifest([Mod("Accessories", Accessories)], modpackFile)];
        using var sync = await Sync();

        Assert.Equal(HttpStatusCode.BadRequest, sync.StatusCode);
        Assert.Empty(Servers.GameServers);
    }

    [Fact]
    public async Task A_mod_without_a_name_is_rejected()
    {
        ManifestProvider.Manifests = [Manifest([Mod(" ", Accessories)])];
        using var sync = await Sync();

        Assert.Equal(HttpStatusCode.BadRequest, sync.StatusCode);
    }

    private Task<HttpResponseMessage> Sync()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiAttribute.HeaderName, ApiWebApplicationFactory.TestInternalToken);
        return client.PostAsync("/internal/gameservers/sync", null);
    }

    private async Task<JsonElement> GetMods(string slug)
    {
        using var client = factory.CreateClientWithRole("READ_ONLY");
        using var response = await client.GetAsync($"/gameservers/{slug}/mods");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static GameServerSyncDto Manifest(
        IReadOnlyList<GameServerModSyncDto>? mods,
        string? modpackFile = null,
        string slug = Slug) => new(
        slug,
        slug == Slug ? "COBBLEMON" : "RUST",
        slug == Slug ? "SOURCE_RCON" : "STEAM_A2S",
        slug == Slug ? "Huiitre Cobblemon Server" : "Huiitre Rust Server PvE",
        null,
        null,
        "172.19.0.7",
        slug == Slug ? 25575 : 28017,
        "games.huiitre.fr",
        slug == Slug ? 25565 : 28015,
        JsonDocument.Parse("{}").RootElement.Clone(),
        mods,
        modpackFile,
        modpackFile is null ? null : 350_604_203L,
        modpackFile is null ? null : "42ecff15");

    private static GameServerModSyncDto Mod(string name, string? url, string? icon = null) =>
        new(name, "1.0.0", url, ["Author"], $"{name}.jar", icon);

    private static async Task AssertReport(HttpResponseMessage response, int created, int updated, int unchanged)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created, report.GetProperty("created").GetInt32());
        Assert.Equal(updated, report.GetProperty("updated").GetInt32());
        Assert.Equal(unchanged, report.GetProperty("unchanged").GetInt32());
    }
}
