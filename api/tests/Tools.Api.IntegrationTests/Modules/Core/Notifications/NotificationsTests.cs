using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.Notifications;

// Routes des notifications de l'utilisateur, reprises de l'API Java.
//
// Ce qui est vérifié ici n'est pas le SQL mais la frontière : qui a le droit d'appeler, et sur
// les lignes de qui. Le destinataire ne circule jamais dans la requête — il vient du jeton — et
// c'est la seule chose qui empêche un appelant de toucher les notifications d'un autre.
public sealed class NotificationsTests : IClassFixture<ApiWebApplicationFactory>
{
    private const long UserId = 42;
    private const long OtherUserId = 43;

    private readonly ApiWebApplicationFactory factory;

    public NotificationsTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        Notifications.Clear();
    }

    private InMemoryNotificationRepository Notifications =>
        factory.Services.GetRequiredService<InMemoryNotificationRepository>();

    [Fact]
    public async Task Listing_notifications_without_a_token_is_refused()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_account_without_any_role_cannot_list_its_notifications()
    {
        using var client = factory.CreateClientForUser(UserId);

        using var response = await client.GetAsync("/notifications");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Listing_returns_only_the_notifications_of_the_caller()
    {
        Notifications.GiveTo(UserId, 1, "La mienne");
        Notifications.GiveTo(OtherUserId, 2, "Celle d'un autre");

        using var client = factory.CreateClientForUser(UserId, "READ_ONLY");

        var views = await client.GetFromJsonAsync<JsonElement>("/notifications");

        var titles = views.EnumerateArray().Select(view => view.GetProperty("title").GetString()).ToList();
        Assert.Equal(["La mienne"], titles);
    }

    [Fact]
    public async Task Marking_some_notifications_as_read_leaves_the_others_untouched()
    {
        Notifications.GiveTo(UserId, 1, "Première");
        Notifications.GiveTo(UserId, 2, "Deuxième");

        using var client = factory.CreateClientForUser(UserId, "READ_ONLY");

        using var response = await client.PatchAsync("/notifications/read?ids=1", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([2], Notifications.UnreadIdsOf(UserId));
    }

    [Fact]
    public async Task Marking_as_read_without_ids_marks_everything()
    {
        Notifications.GiveTo(UserId, 1, "Première");
        Notifications.GiveTo(UserId, 2, "Deuxième");

        using var client = factory.CreateClientForUser(UserId, "READ_ONLY");

        using var response = await client.PatchAsync("/notifications/read", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(Notifications.UnreadIdsOf(UserId));
    }

    [Fact]
    public async Task Identifiers_that_are_not_numbers_are_refused()
    {
        using var client = factory.CreateClientForUser(UserId, "READ_ONLY");

        using var response = await client.PatchAsync("/notifications/read?ids=abc", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_NOTIFICATION_IDS", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Deleting_removes_only_the_designated_notifications_of_the_caller()
    {
        Notifications.GiveTo(UserId, 1, "Première");
        Notifications.GiveTo(UserId, 2, "Deuxième");
        Notifications.GiveTo(OtherUserId, 1, "Celle d'un autre");

        using var client = factory.CreateClientForUser(UserId, "READ_ONLY");

        using var response = await client.DeleteAsync("/notifications?ids=1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal([2], Notifications.IdsOf(UserId));

        // L'identifiant 1 existe aussi chez l'autre utilisateur : la suppression ne l'a pas suivi.
        Assert.Equal([1], Notifications.IdsOf(OtherUserId));
    }

    [Fact]
    public async Task Deleting_without_ids_empties_the_history_of_the_caller()
    {
        Notifications.GiveTo(UserId, 1, "Première");
        Notifications.GiveTo(UserId, 2, "Deuxième");

        using var client = factory.CreateClientForUser(UserId, "READ_ONLY");

        using var response = await client.DeleteAsync("/notifications");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(Notifications.IdsOf(UserId));
    }

    [Fact]
    public async Task Sending_a_notification_by_hand_requires_TECH()
    {
        using var client = factory.CreateClientForUser(UserId, "USER");

        using var response = await client.PostAsJsonAsync("/notifications", ManualPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(Notifications.Notifications);
    }

    [Fact]
    public async Task A_TECH_account_can_send_a_notification_by_hand()
    {
        using var client = factory.CreateClientForUser(UserId, "TECH");

        using var response = await client.PostAsJsonAsync("/notifications", ManualPayload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt64() > 0);

        var notification = Assert.Single(Notifications.Notifications);
        Assert.Equal("Maintenance", notification.Title);
    }

    private static object ManualPayload => new
    {
        title = "Maintenance",
        body = "L'API sera indisponible ce soir.",
        type = "WARNING",
        targetUserId = OtherUserId
    };
}
