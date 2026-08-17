using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.ApiCore.IntegrationTests.Fakes;
using Tools.ApiCore.IntegrationTests.Fixtures;
using Tools.ApiCore.Modules.Common.Api.Internal;
using Xunit;

namespace Tools.ApiCore.IntegrationTests.Modules.Notifications;

// Route de service à service : elle n'est protégée ni par un jeton utilisateur, ni par le
// réseau, mais par un secret partagé. Ces tests vérifient qu'aucun autre chemin n'y mène.
public sealed class InternalNotificationsTests : IClassFixture<ApiCoreWebApplicationFactory>
{
    private readonly ApiCoreWebApplicationFactory factory;

    public InternalNotificationsTests(ApiCoreWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.Services.GetRequiredService<InMemoryNotificationRepository>().Clear();
    }

    private InMemoryNotificationRepository Notifications =>
        factory.Services.GetRequiredService<InMemoryNotificationRepository>();

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
        var client = ClientWithToken(ApiCoreWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("id").GetInt64() > 0);

        var notification = Assert.Single(Notifications.Notifications);
        Assert.Equal("Sync terminée", notification.Title);
        Assert.Equal("SUCCESS", notification.Type);
        Assert.Equal(["ADMIN", "OWNER"], Notifications.RoleCodesAsked);
    }

    // 404 et non 401 : la réponse ne doit pas confirmer que la route existe.
    [Fact]
    public async Task Publishing_without_the_header_is_not_found()
    {
        using var response = await factory.CreateClient()
            .PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(Notifications.Notifications);
    }

    [Fact]
    public async Task Publishing_with_a_wrong_secret_is_not_found()
    {
        var client = ClientWithToken("mauvais-secret");

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(Notifications.Notifications);
    }

    // Un préfixe correct ne doit pas davantage passer : la comparaison porte sur la valeur
    // entière, pas sur un début commun.
    [Fact]
    public async Task Publishing_with_a_prefix_of_the_secret_is_not_found()
    {
        var client = ClientWithToken(ApiCoreWebApplicationFactory.TestInternalToken[..10]);

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Le jeton utilisateur ne remplace pas le secret interne : ce sont deux choses distinctes.
    [Fact]
    public async Task An_administrator_token_does_not_open_the_internal_route()
    {
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.PostAsJsonAsync("/internal/notifications", ValidPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_without_a_target_is_refused()
    {
        var client = ClientWithToken(ApiCoreWebApplicationFactory.TestInternalToken);

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
        var client = ClientWithToken(ApiCoreWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/notifications", new
        {
            title = "Rôle inconnu",
            body = "corps",
            targetMinRole = "SUPER_ADMIN"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
