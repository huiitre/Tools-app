using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;
using Tools.Api.IntegrationTests.Fixtures;

namespace Tools.Api.IntegrationTests.Modules.Core.Security;

// Règles d'autorisation transverses, portées par UseCaseAuthorizer et RoleCodes : elles
// valent pour tout use case sécurisé, quel que soit le rôle qu'il exige.
//
// Les deux routes utilisées ne sont que des supports, choisis pour leurs exigences
// opposées : `/auth/password` demande le plancher READ_ONLY, `/mail` demande TECH. Ce que
// chaque use case exige lui est propre et se teste dans son module.
public sealed class AuthorizationTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string LowestRequirementRoute = "/auth/password";
    private const string TechnicalRequirementRoute = "/mail";

    private static readonly object MailPayload = new
    {
        to = new[] { "user@example.com" },
        subject = "Sujet",
        text = "Corps du message."
    };

    private readonly ApiWebApplicationFactory factory;

    public AuthorizationTests(ApiWebApplicationFactory factory)
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
        var client = factory.CreateClientWithRole("SUPER_ADMIN");

        using var response = await client.PostAsJsonAsync(TechnicalRequirementRoute, MailPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    [Fact]
    public async Task A_token_carrying_the_former_roles_array_is_still_honoured()
    {
        // Un utilisateur ne porte qu'un rôle global, et l'API n'émet plus que le claim `role`.
        // Les jetons émis avant ce changement portent un claim `roles` en tableau et restent
        // valides jusqu'à leur expiration : les refuser déconnecterait tout le monde au
        // déploiement. La tolérance vit ici, à la lecture du jeton, et nulle part ailleurs.
        var client = factory.CreateClientWithForgedClaims(
            1,
            new Claim("roles", """["TECH"]""", JsonClaimValueTypes.JsonArray));

        using var response = await client.PostAsJsonAsync(TechnicalRequirementRoute, MailPayload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task The_current_role_claim_wins_over_the_former_array()
    {
        // Cas de bascule : un jeton peut théoriquement porter les deux formes. C'est la forme
        // actuelle qui décide, sans quoi la tolérance héritée pourrait accorder un droit que le
        // rôle actuel ne donne plus.
        var client = factory.CreateClientWithForgedClaims(
            1,
            new Claim("role", "READ_ONLY"),
            new Claim("roles", """["TECH"]""", JsonClaimValueTypes.JsonArray));

        using var response = await client.PostAsJsonAsync(TechnicalRequirementRoute, MailPayload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("INSUFFICIENT_ROLE", await ReadCode(response));
    }

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
