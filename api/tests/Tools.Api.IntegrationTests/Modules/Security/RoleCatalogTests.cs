using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tools.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Security;

// Catalogue des rôles attribuables.
//
// Le contrôleur Java annote `@RequiredRole(TECH)` alors que son use case déclare ADMIN.
// L'annotation n'étant lue par aucun aspect, c'est ADMIN qui s'applique réellement — et donc
// ADMIN qui est reproduit ici. Un TECH est refusé, puisqu'il est sous ADMIN.
public sealed class RoleCatalogTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Listing_roles_returns_the_catalog()
    {
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.GetAsync("/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(6, roles.GetArrayLength());
        Assert.Equal("READ_ONLY", roles[0].GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    [InlineData("TECH")]
    public async Task Listing_roles_is_refused_below_the_administration_level(string role)
    {
        var client = factory.CreateClientWithRoles(role);

        using var response = await client.GetAsync("/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INSUFFICIENT_ROLE", problem.GetProperty("code").GetString());
    }
}
