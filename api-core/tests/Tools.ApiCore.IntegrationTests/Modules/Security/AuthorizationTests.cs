using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Tools.ApiCore.IntegrationTests.Fixtures;

namespace Tools.ApiCore.IntegrationTests.Modules.Security;

// Règles d'autorisation transverses, portées par UseCaseAuthorizer et RoleCodes : elles
// valent pour tout use case sécurisé, quel que soit le rôle qu'il exige.
//
// Les deux routes utilisées ne sont que des supports, choisis pour leurs exigences
// opposées : `/users/password` demande le plancher READ_ONLY, `/mail` demande TECH. Ce que
// chaque use case exige lui est propre et se teste dans son module.
public sealed class AuthorizationTests : IClassFixture<ApiCoreWebApplicationFactory>
{
    private const string LowestRequirementRoute = "/users/password";
    private const string TechnicalRequirementRoute = "/mail";

    private static readonly object MailPayload = new
    {
        to = new[] { "user@example.com" },
        subject = "Sujet",
        text = "Corps du message."
    };

    private readonly ApiCoreWebApplicationFactory factory;

    public AuthorizationTests(ApiCoreWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.Store.Reset();
    }

    [Fact]
    public async Task A_token_without_any_role_is_refused_even_by_the_lowest_requirement()
    {
        // Aucun rôle ne satisfait jamais aucune exigence : être authentifié ne suffit pas,
        // même pour un use case ouvert au rôle le plus bas.
        factory.Store.AddUser(1, "user@example.com", withPasswordProvider: true);
        var client = factory.CreateClientForUser(1);

        using var response = await client.PatchAsJsonAsync(
            LowestRequirementRoute, new { password = "peu-importe" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task An_unknown_role_code_never_grants_access()
    {
        // Un code absent de l'énumération est ignoré, jamais interprété favorablement :
        // un jeton forgé avec un rôle inventé ne vaut pas mieux qu'un jeton sans rôle.
        var client = factory.CreateClientWithRoles("SUPER_ADMIN");

        using var response = await client.PostAsJsonAsync(TechnicalRequirementRoute, MailPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task The_most_permissive_role_of_the_token_decides()
    {
        // Un utilisateur qui cumule des rôles est jugé sur le plus élevé, et l'ordre dans
        // lequel ils apparaissent dans le jeton n'a aucune importance.
        var client = factory.CreateClientWithRoles("USER", "ADMIN", "READ_ONLY");

        using var response = await client.PostAsJsonAsync(TechnicalRequirementRoute, MailPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
