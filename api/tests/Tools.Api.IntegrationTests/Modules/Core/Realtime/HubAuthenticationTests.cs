using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Auth.Application.Services;
using Tools.Api.Modules.Core.Auth.Domain;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.Realtime;

// Le navigateur ne peut pas poser d'en-tête sur la poignée de main WebSocket : le jeton du hub
// voyage dans l'URL. Le schéma par défaut, qui choisit entre JWT et clé d'API, doit le laisser
// passer au JWT.
public sealed class HubAuthenticationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private string AccessToken() => factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(
        new AuthUser(1, "admin@example.com", true, "HUMAN"), "USER", new Dictionary<string, string>());

    [Fact]
    public async Task The_hub_accepts_the_token_carried_in_the_query_string()
    {
        using var response = await factory.CreateClient()
            .PostAsync($"/hub/negotiate?negotiateVersion=1&access_token={AccessToken()}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_hub_refuses_an_anonymous_negotiation()
    {
        using var response = await factory.CreateClient().PostAsync("/hub/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
