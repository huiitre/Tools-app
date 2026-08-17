using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.ApiCore.IntegrationTests.Fakes;
using Tools.ApiCore.IntegrationTests.Fixtures;
using Xunit;

namespace Tools.ApiCore.IntegrationTests.Modules.Access;

// Modules fonctionnels et accès des utilisateurs à ces modules.
//
// Tout exige ADMIN, y compris la gestion du catalogue : l'API Java annote TECH sur ces
// contrôleurs, mais l'annotation est inopérante et ses use cases déclarent ADMIN.
public sealed class ModuleAccessTests(ApiCoreWebApplicationFactory factory)
    : IClassFixture<ApiCoreWebApplicationFactory>
{
    private const long ModuleId = InMemoryModuleRepository.ExistingModuleId;
    private const long UserId = InMemoryUserRepository.ExistingUserId;

    private InMemoryModuleMembershipRepository Memberships =>
        factory.Services.GetRequiredService<InMemoryModuleMembershipRepository>();

    [Fact]
    public async Task Listing_modules_returns_the_catalog()
    {
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.GetAsync("/modules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var modules = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(InMemoryModuleRepository.ExistingModuleCode, modules[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_created_module_starts_inactive()
    {
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.PostAsJsonAsync("/modules", new
        {
            code = $"module-{Guid.NewGuid():N}",
            name = "Nouveau module",
            description = "Créé par le test"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var listResponse = await client.GetAsync("/modules");
        var modules = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var created = modules.EnumerateArray().Last();
        Assert.False(created.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Creating_a_module_with_an_existing_code_conflicts()
    {
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.PostAsJsonAsync("/modules", new
        {
            code = InMemoryModuleRepository.ExistingModuleCode,
            name = "Doublon"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("MODULE_CODE_ALREADY_EXISTS", await ReadCode(response));
    }

    [Fact]
    public async Task Granting_access_gives_the_read_only_role()
    {
        Memberships.Reset();
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.PostAsync($"/modules/{ModuleId}/users/{UserId}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // READ_ONLY vaut 1 dans le référentiel : entrer dans un module ne donne aucun pouvoir.
        Assert.Equal(1, Memberships.RoleOf(ModuleId, UserId));
    }

    [Fact]
    public async Task Granting_an_access_twice_conflicts()
    {
        Memberships.Reset();
        var client = factory.CreateClientWithRoles("ADMIN");

        using var first = await client.PostAsync($"/modules/{ModuleId}/users/{UserId}", null);
        using var second = await client.PostAsync($"/modules/{ModuleId}/users/{UserId}", null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("USER_ALREADY_HAS_ACCESS_TO_MODULE", await ReadCode(second));
    }

    [Fact]
    public async Task Changing_a_role_requires_an_existing_access()
    {
        Memberships.Reset();
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.PutAsJsonAsync(
            $"/modules/{ModuleId}/users/{UserId}/role", new { roleId = 2 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("USER_MODULE_ROLE_NOT_FOUND", await ReadCode(response));
    }

    [Fact]
    public async Task Changing_a_role_replaces_the_previous_one()
    {
        Memberships.Reset();
        var client = factory.CreateClientWithRoles("ADMIN");
        await client.PostAsync($"/modules/{ModuleId}/users/{UserId}", null);

        using var response = await client.PutAsJsonAsync(
            $"/modules/{ModuleId}/users/{UserId}/role", new { roleId = 2 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(2, Memberships.RoleOf(ModuleId, UserId));
    }

    [Fact]
    public async Task Revoking_an_access_removes_the_membership()
    {
        Memberships.Reset();
        var client = factory.CreateClientWithRoles("ADMIN");
        await client.PostAsync($"/modules/{ModuleId}/users/{UserId}", null);

        using var response = await client.DeleteAsync($"/modules/{ModuleId}/users/{UserId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(Memberships.RoleOf(ModuleId, UserId));
    }

    [Fact]
    public async Task Listing_members_of_an_unknown_module_is_not_found()
    {
        var client = factory.CreateClientWithRoles("ADMIN");

        using var response = await client.GetAsync("/modules/999/users");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("MODULE_NOT_FOUND", await ReadCode(response));
    }

    // Toutes les routes du module, y compris la gestion du catalogue, sont fermées sous ADMIN.
    // TECH y est refusé comme les autres : il est sous ADMIN dans la hiérarchie.
    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    [InlineData("TECH")]
    public async Task Every_route_is_refused_below_the_administration_level(string role)
    {
        var client = factory.CreateClientWithRoles(role);

        var responses = new[]
        {
            await client.GetAsync("/modules"),
            await client.PostAsJsonAsync("/modules", new { code = "x", name = "x" }),
            await client.PutAsJsonAsync($"/modules/{ModuleId}", new { code = "x", name = "x", active = true }),
            await client.GetAsync($"/modules/{ModuleId}/users"),
            await client.PostAsync($"/modules/{ModuleId}/users/{UserId}", null),
            await client.PutAsJsonAsync($"/modules/{ModuleId}/users/{UserId}/role", new { roleId = 2 }),
            await client.DeleteAsync($"/modules/{ModuleId}/users/{UserId}")
        };

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
            response.Dispose();
        }
    }

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
