using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.Notifications;

// Ancienne URL des services, gardée le temps de leur migration vers les clés d'API. Elle sert le
// même use case que POST /notifications : TECH, ou une clé (l'ancien secret partagé en tenant
// lieu, avec tous les scopes). Ces tests vérifient que le secret marche encore et que rien
// d'autre n'y mène.
public sealed class InternalNotificationsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public InternalNotificationsTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.Services.GetRequiredService<InMemoryNotificationRepository>().Clear();
        factory.Services.GetRequiredService<RecordingRealtimePublisher>().Clear();
    }

    private InMemoryNotificationRepository Notifications =>
        factory.Services.GetRequiredService<InMemoryNotificationRepository>();

    private RecordingRealtimePublisher RealtimePublisher =>
        factory.Services.GetRequiredService<RecordingRealtimePublisher>();

    private static object ValidPayload => new
    {
        title = "Sync terminée",
        body = "L'import Palworld s'est terminé.",
        type = "SUCCESS",
        targetMinRole = "ADMIN"
    };

    private HttpClient ClientWithToken(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiAttribute.HeaderName, token);
        return client;
    }

    [Fact]
    public async Task Publishing_with_the_shared_secret_records_the_notification_and_returns_its_id()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt64() > 0);

        var notification = Assert.Single(Notifications.Notifications);
        Assert.Equal("Sync terminée", notification.Title);
        Assert.Equal("SUCCESS", notification.Type);
        Assert.Equal(["ADMIN", "OWNER"], Notifications.RoleCodesAsked);

        var push = RealtimePublisher.LastPublish;
        Assert.NotNull(push);
        Assert.Equal("Core.ReceiveNotification", push!.EventType);
        Assert.Equal([InMemoryNotificationRepository.AdminUserId], push.UserIds);
    }

    [Fact]
    public async Task Publishing_without_any_credential_is_unauthorized()
    {
        using var response = await factory.CreateClient()
            .PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(Notifications.Notifications);
    }

    [Fact]
    public async Task Publishing_with_a_wrong_secret_is_unauthorized()
    {
        var client = ClientWithToken("mauvais-secret");

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(Notifications.Notifications);
    }

    // Un préfixe correct ne doit pas davantage passer : la comparaison porte sur la valeur
    // entière, pas sur un début commun.
    [Fact]
    public async Task Publishing_with_a_prefix_of_the_secret_is_unauthorized()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken[..10]);

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Même use case que POST /notifications : un utilisateur TECH y a droit.
    [Fact]
    public async Task A_technical_token_opens_the_route()
    {
        var client = factory.CreateClientWithRole("TECH");

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_user_token_is_forbidden()
    {
        var client = factory.CreateClientWithRole("USER");

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(Notifications.Notifications);
    }

    [Fact]
    public async Task Publishing_with_a_module_id_notifies_the_module_members()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/notifications", new
        {
            title = "Mise à jour Dofus",
            body = "Les assets ont été synchronisés.",
            type = "SUCCESS",
            targetModuleId = 3
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, Notifications.ModuleIdAsked);

        var notification = Assert.Single(Notifications.Notifications);
        Assert.Equal(3, notification.TargetModuleId);
    }

    [Fact]
    public async Task Publishing_without_a_target_is_refused()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/notifications", new
        {
            title = "Sans cible",
            body = "corps"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_with_an_unknown_role_is_refused()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/notifications", new
        {
            title = "Rôle inconnu",
            body = "corps",
            targetMinRole = "SUPER_ADMIN"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
