using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Common.Api.Internal;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Realtime;

// Route de service à service : elle n'est protégée ni par un jeton utilisateur, ni par le
// réseau, mais par un secret partagé — même modèle que /internal/notifications.
public sealed class InternalRealtimePublishTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory factory;

    public InternalRealtimePublishTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.Services.GetRequiredService<RecordingRealtimePublisher>().Clear();
        factory.Services.GetRequiredService<InMemoryRecipientResolver>().Clear();
    }

    private RecordingRealtimePublisher Publisher =>
        factory.Services.GetRequiredService<RecordingRealtimePublisher>();

    private static object ValidPayload => new
    {
        eventType = "ReceiveGameEvent",
        payload = new { action = "join", playerId = 42 },
        targetUserId = 7
    };

    private HttpClient ClientWithToken(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiAttribute.HeaderName, token);
        return client;
    }

    [Fact]
    public async Task Publishing_with_an_explicit_user_id_relays_to_that_user()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/realtime/publish", ValidPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var push = Publisher.LastPublish;
        Assert.NotNull(push);
        Assert.Equal("ReceiveGameEvent", push!.EventType);
        Assert.Equal([7], push.UserIds);
    }

    [Fact]
    public async Task Publishing_with_an_explicit_user_id_list_does_not_ask_the_resolver()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/realtime/publish", new
        {
            eventType = "ReceiveChatMessage",
            payload = new { text = "gg" },
            targetUserIds = new[] { 10, 11, 12 }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var push = Publisher.LastPublish;
        Assert.NotNull(push);
        Assert.Equal([10, 11, 12], push!.UserIds);
    }

    [Fact]
    public async Task Publishing_with_a_module_id_resolves_its_members()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/realtime/publish", new
        {
            eventType = "ReceiveModuleEvent",
            payload = new { },
            targetModuleId = 8
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var resolver = factory.Services.GetRequiredService<InMemoryRecipientResolver>();
        Assert.Equal(8, resolver.ModuleIdAsked);

        var push = Publisher.LastPublish;
        Assert.NotNull(push);
        Assert.Equal([InMemoryRecipientResolver.ModuleMemberUserId], push!.UserIds);
    }

    // 404 et non 401 : la réponse ne doit pas confirmer que la route existe.
    [Fact]
    public async Task Publishing_without_the_header_is_not_found()
    {
        using var response = await factory.CreateClient()
            .PostAsJsonAsync("/internal/realtime/publish", ValidPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(Publisher.LastPublish);
    }

    [Fact]
    public async Task Publishing_with_a_wrong_secret_is_not_found()
    {
        var client = ClientWithToken("mauvais-secret");

        using var response = await client.PostAsJsonAsync("/internal/realtime/publish", ValidPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(Publisher.LastPublish);
    }

    // Le jeton utilisateur ne remplace pas le secret interne : ce sont deux choses distinctes.
    [Fact]
    public async Task An_administrator_token_does_not_open_the_internal_route()
    {
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.PostAsJsonAsync("/internal/realtime/publish", ValidPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_without_a_target_is_refused()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/realtime/publish", new
        {
            eventType = "ReceiveGameEvent",
            payload = new { }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_with_an_unknown_role_is_refused()
    {
        var client = ClientWithToken(ApiWebApplicationFactory.TestInternalToken);

        using var response = await client.PostAsJsonAsync("/internal/realtime/publish", new
        {
            eventType = "ReceiveGameEvent",
            payload = new { },
            targetMinRole = "SUPER_ADMIN"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
