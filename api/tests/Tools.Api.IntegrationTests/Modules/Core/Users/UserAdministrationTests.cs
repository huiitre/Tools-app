using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Tools.Api.IntegrationTests.Modules.Core.Users;

// Administration des utilisateurs : liste, attribution du rôle global et statut du compte.
//
// Le rôle exigé est ADMIN, comme dans l'API Java. Attention au piège : le contrôleur Java
// annote parfois TECH, mais cette annotation n'est lue par aucun aspect — seul
// `requiredRole()` du use case s'applique.
public sealed class UserAdministrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private const long ExistingUserId = InMemoryUserRepository.ExistingUserId;
    private static readonly object AdminRole = new { roleId = 4 };

    [Fact]
    public async Task Listing_users_returns_the_administration_rows()
    {
        var client = factory.CreateClientWithRole("ADMIN");

        using var response = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ExistingUserId, users[0].GetProperty("id").GetInt64());
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("OWNER")]
    public async Task Listing_users_is_allowed_from_the_administration_level(string role)
    {
        var client = factory.CreateClientWithRole(role);

        using var response = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // TECH est explicitement refusé : la hiérarchie le place *sous* ADMIN (voir RoleCode).
    // L'ordre de déclaration de l'énumération Java suggère l'inverse, mais c'est
    // RoleHierarchy qui fait autorité là-bas, et il donne TECH=4 et ADMIN=5.
    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    [InlineData("TECH")]
    public async Task Listing_users_is_refused_below_the_administration_level(string role)
    {
        var client = factory.CreateClientWithRole(role);

        using var response = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task Setting_a_global_role_replaces_the_previous_one()
    {
        var client = factory.CreateClientWithRole("ADMIN");

        using var response = await client.PutAsJsonAsync($"/users/{ExistingUserId}/role", AdminRole);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var repository = factory.Services.GetRequiredService<InMemoryUserRepository>();
        Assert.Equal(ExistingUserId, repository.LastRoleAssignedTo);
        Assert.Equal(4, repository.LastRoleAssigned);
    }

    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    [InlineData("TECH")]
    public async Task Setting_a_global_role_is_refused_below_the_administration_level(string role)
    {
        var client = factory.CreateClientWithRole(role);

        using var response = await client.PutAsJsonAsync($"/users/{ExistingUserId}/role", AdminRole);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task Setting_a_role_on_an_unknown_user_is_not_found()
    {
        var client = factory.CreateClientWithRole("ADMIN");

        using var response = await client.PutAsJsonAsync("/users/999/role", AdminRole);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("USER_NOT_FOUND", await ReadCode(response));
    }

    [Fact]
    public async Task Setting_an_unknown_role_is_not_found()
    {
        var client = factory.CreateClientWithRole("ADMIN");

        using var response = await client.PutAsJsonAsync(
            $"/users/{ExistingUserId}/role", new { roleId = 999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("ROLE_NOT_FOUND", await ReadCode(response));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Setting_the_active_status_records_the_requested_state(bool active)
    {
        // L'acteur n'est pas la cible : la garde anti-auto-suspension n'entre pas en jeu ici.
        var client = factory.CreateClientForUser(2, "ADMIN");

        using var response = await client.PutAsJsonAsync(
            $"/users/{ExistingUserId}/active", new { active });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var repository = factory.Services.GetRequiredService<InMemoryUserRepository>();
        Assert.Equal(ExistingUserId, repository.LastActiveSetOn);
        Assert.Equal(active, repository.LastActiveSet);
    }

    // Se suspendre soi-même retirerait le droit d'annuler l'opération : il faudrait un autre
    // administrateur, ou un UPDATE à la main en base.
    [Fact]
    public async Task Deactivating_your_own_account_is_a_conflict()
    {
        var client = factory.CreateClientForUser(ExistingUserId, "ADMIN");

        using var response = await client.PutAsJsonAsync(
            $"/users/{ExistingUserId}/active", new { active = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CANNOT_DEACTIVATE_SELF", await ReadCode(response));
    }

    // La réactivation de son propre compte ne ferme aucune porte : rien à interdire.
    [Fact]
    public async Task Reactivating_your_own_account_is_allowed()
    {
        var client = factory.CreateClientForUser(ExistingUserId, "ADMIN");

        using var response = await client.PutAsJsonAsync(
            $"/users/{ExistingUserId}/active", new { active = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("READ_ONLY")]
    [InlineData("USER")]
    [InlineData("MODERATOR")]
    [InlineData("TECH")]
    public async Task Setting_the_active_status_is_refused_below_the_administration_level(string role)
    {
        var client = factory.CreateClientForUser(2, role);

        using var response = await client.PutAsJsonAsync(
            $"/users/{ExistingUserId}/active", new { active = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task Setting_the_active_status_on_an_unknown_user_is_not_found()
    {
        var client = factory.CreateClientForUser(2, "ADMIN");

        using var response = await client.PutAsJsonAsync("/users/999/active", new { active = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("USER_NOT_FOUND", await ReadCode(response));
    }

    // Sans ce refus, un corps où le champ manque vaudrait `false` et suspendrait le compte.
    [Fact]
    public async Task Setting_the_active_status_without_the_field_is_rejected()
    {
        var client = factory.CreateClientForUser(2, "ADMIN");

        using var response = await client.PutAsJsonAsync($"/users/{ExistingUserId}/active", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
