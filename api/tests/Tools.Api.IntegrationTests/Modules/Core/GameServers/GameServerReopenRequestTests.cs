using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Usecases;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.GameServers;

// Un utilisateur connecté demande la réouverture d'un serveur hors ligne : les administrateurs
// sont notifiés, une fois par heure et par serveur au plus.
public sealed class GameServerReopenRequestTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string Slug = "minecraft-test";
    private readonly ApiWebApplicationFactory factory;

    public GameServerReopenRequestTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Servers.Clear();
        Notifications.Clear();
        Reopens.Clear(Slug);
        Servers.UpsertAsync(new GameServerSyncEntry(
            Slug, "RUST", "STEAM_A2S", "Serveur Minecraft", null, "172.19.0.4", 25575, "{}",
            "Minecraft", null, false, true, "games.huiitre.fr", 25565, [], null, null)).GetAwaiter().GetResult();
    }

    private InMemoryGameServerRepository Servers => factory.Services.GetRequiredService<InMemoryGameServerRepository>();
    private InMemoryNotificationRepository Notifications => factory.Services.GetRequiredService<InMemoryNotificationRepository>();
    private IGameServerLiveStateStore Live => factory.Services.GetRequiredService<IGameServerLiveStateStore>();
    private IGameServerReopenRequestStore Reopens => factory.Services.GetRequiredService<IGameServerReopenRequestStore>();
    private RecordingRealtimePublisher Realtime => factory.Services.GetRequiredService<RecordingRealtimePublisher>();

    private void SetOnline(bool online) =>
        Live.Set(new GameServerLiveSnapshot(Slug, online, online ? 2 : null, 10, DateTime.UtcNow, null));

    private Task<HttpResponseMessage> Request(string role = "READ_ONLY") =>
        factory.CreateClientWithRole(role).PostAsync($"/gameservers/{Slug}/reopen-request", null);

    private static async Task<string?> Code(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("code").GetString();

    [Fact]
    public async Task An_offline_server_notifies_the_administrators_with_the_requester_name()
    {
        SetOnline(false);

        using var response = await Request();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notification = Assert.Single(Notifications.Notifications);
        Assert.Equal("Demande de réouverture", notification.Title);
        Assert.Equal("Admin a demandé la réouverture du serveur Serveur Minecraft (Minecraft).", notification.Body);
        Assert.Contains("ADMIN", Notifications.RoleCodesAsked);
        Assert.Contains("OWNER", Notifications.RoleCodesAsked);
        Assert.DoesNotContain("TECH", Notifications.RoleCodesAsked);

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var requestedAt = body.GetProperty("requestedAt").GetDateTime();
        Assert.Equal(requestedAt.AddHours(1), body.GetProperty("nextRequestAt").GetDateTime());
        Assert.Equal(requestedAt, Live.GetAll().Single(s => s.Slug == Slug).ReopenRequestedAt);
    }

    [Fact]
    public async Task A_second_request_within_the_hour_is_refused_and_notifies_nobody()
    {
        SetOnline(false);
        using var first = await Request();

        using var second = await Request("ADMIN");

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("GAME_SERVER_REOPEN_ALREADY_REQUESTED", await Code(second));
        Assert.Single(Notifications.Notifications);
    }

    [Fact]
    public async Task An_online_server_cannot_be_reported()
    {
        SetOnline(true);

        using var response = await Request();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("GAME_SERVER_NOT_OFFLINE", await Code(response));
        Assert.Empty(Notifications.Notifications);
    }

    [Fact]
    public async Task An_unknown_server_is_not_found()
    {
        using var response = await factory.CreateClientWithRole("USER").PostAsync("/gameservers/inconnu/reopen-request", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_anonymous_caller_is_refused()
    {
        SetOnline(false);

        using var response = await factory.CreateClient().PostAsync($"/gameservers/{Slug}/reopen-request", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // De retour en ligne, la demande est satisfaite : la prochaine panne se signale sans attendre l'heure.
    [Fact]
    public async Task Coming_back_online_clears_the_request()
    {
        SetOnline(false);
        using var first = await Request();
        Provider.Status = new GameServerStatus(true, 1, 10);
        Provider.ShouldThrow = false;

        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<PollGameServersUseCase>().Execute(CancellationToken.None);

        Assert.Null(Reopens.Get(Slug));
        SetOnline(false);
        using var again = await Request();
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(2, Notifications.Notifications.Count);
    }

    private FakeGameServerProvider Provider => factory.Services.GetRequiredService<FakeGameServerProvider>();
}
