using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tools.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.Admin;

// Tableau de bord d'administration.
public sealed class AdminStatsTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Stats_return_the_dashboard_indicators()
    {
        var client = factory.CreateClientWithRole("ADMIN");

        using var response = await client.GetAsync("/admin/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(12, stats.GetProperty("totalUsers").GetInt64());
        Assert.Equal(9, stats.GetProperty("activeUsers").GetInt64());
        Assert.Equal(3, stats.GetProperty("newUsersThisWeek").GetInt64());

        // Le module est identifié par son code, comme dans l'API Java — le champ `moduleId`
        // déclaré côté frontend n'a jamais existé dans la réponse.
        var perModule = stats.GetProperty("usersPerModule")[0];
        Assert.Equal("dofus", perModule.GetProperty("moduleCode").GetString());
        Assert.Equal(7, perModule.GetProperty("userCount").GetInt64());
    }

    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    [InlineData("TECH")]
    public async Task Stats_are_refused_below_the_administration_level(string role)
    {
        var client = factory.CreateClientWithRole(role);

        using var response = await client.GetAsync("/admin/stats");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INSUFFICIENT_ROLE", problem.GetProperty("code").GetString());
    }
}
