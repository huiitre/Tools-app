using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;
using Tools.Api.IntegrationTests.Fixtures;

namespace Tools.Api.IntegrationTests.Modules.Core.Security;

// Autorisation d'un use case rattaché à un module : le rôle exigé se lit dans ce module, et
// nulle part ailleurs. La règle est celle de l'API Java, reprise telle quelle — un droit posé
// sur un module ne veut dire quelque chose que s'il tient face à un rôle global élevé.
//
// La route support est la sonde mappée en environnement Testing (voir SecurityModule) : elle
// exige USER dans le module `todolist`. Aucun use case du Core n'appartient à un module, les
// premiers viendront avec le métier migré depuis Java.
public sealed class ModuleAuthorizationTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string ModuleRoute = "/_tests/module-authorization";
    private const string ModuleCode = "todolist";

    private readonly ApiWebApplicationFactory factory;

    public ModuleAuthorizationTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.Store.Reset();
    }

    [Fact]
    public async Task The_module_role_grants_the_access()
    {
        var client = ClientWithModuleRoles(new() { [ModuleCode] = "USER" });

        using var response = await client.GetAsync(ModuleRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("USER", body.GetProperty("moduleRole").GetString());
    }

    [Fact]
    public async Task A_site_administrator_without_the_module_is_refused()
    {
        // Le cœur de la règle : un rôle global, si élevé soit-il, n'ouvre aucun module. Sans
        // cela, l'administration du site deviendrait un passe-partout métier.
        var client = factory.CreateClientWithRole("ADMIN");

        using var response = await client.GetAsync(ModuleRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NO_MODULE_ACCESS", await ReadCode(response));
    }

    [Fact]
    public async Task A_site_administrator_is_bound_by_his_role_inside_the_module()
    {
        // L'autre moitié de la règle : présent dans le module, l'administrateur du site y vaut
        // ce que son rôle de module dit, et rien de plus.
        var client = ClientWithModuleRoles(new() { [ModuleCode] = "READ_ONLY" }, "ADMIN");

        using var response = await client.GetAsync(ModuleRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task A_token_carrying_the_former_role_array_per_module_is_still_honoured()
    {
        // `user_module_role` a pour clé primaire (user_id, module_id) depuis V2.4.0 : un
        // utilisateur n'y détient qu'un rôle, et le claim ne porte plus qu'une chaîne. Les
        // jetons émis quand il portait un tableau restent lisibles jusqu'à leur expiration.
        var client = factory.CreateClientWithForgedClaims(
            1,
            new Claim("modules", $$"""{"{{ModuleCode}}":["READ_ONLY","ADMIN","USER"]}""", JsonClaimValueTypes.Json));

        using var response = await client.GetAsync(ModuleRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ADMIN", body.GetProperty("moduleRole").GetString());
    }

    [Fact]
    public async Task A_role_held_on_another_module_never_counts()
    {
        var client = ClientWithModuleRoles(new() { ["dofus"] = "ADMIN" });

        using var response = await client.GetAsync(ModuleRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NO_MODULE_ACCESS", await ReadCode(response));
    }

    [Fact]
    public async Task An_unknown_module_or_role_code_never_grants_access()
    {
        // Ni un module absent de l'énumération ni un rôle inventé ne valent un droit : un jeton
        // forgé ne doit pas pouvoir se fabriquer un accès par un code que le Core ignore.
        var client = ClientWithModuleRoles(new()
        {
            ["module_inexistant"] = "ADMIN",
            [ModuleCode] = "SUPER_ADMIN"
        });

        using var response = await client.GetAsync(ModuleRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("NO_MODULE_ACCESS", await ReadCode(response));
    }

    private HttpClient ClientWithModuleRoles(
        Dictionary<string, string> moduleRoles,
        string? role = null) =>
        factory.CreateClientForUser(1, moduleRoles, role);

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
