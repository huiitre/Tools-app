using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Tools.Api.IntegrationTests.Fakes;
using Tools.Api.IntegrationTests.Fixtures;
using Tools.Api.Modules.Core.Auth.Application.Services;
using Tools.Api.Modules.Core.Auth.Domain;
using Tools.Api.Modules.Core.Auth.Infrastructure.Jwt;

namespace Tools.Api.IntegrationTests.Modules.Core.Auth;

// Règles d'authentification transverses : elles valent pour toute route protégée, quel
// que soit le module. `/auth/password` ne sert ici que de route protégée témoin — ce
// qui est vérifié n'a rien à voir avec le mot de passe.
//
// Ces tests existent surtout pour survivre à l'ajout du middleware JwtBearer : ils
// décrivent ce que l'authentification doit continuer de refuser une fois la validation
// du jeton déplacée dans le pipeline ASP.NET.
public sealed class AuthenticationTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string ProtectedRoute = "/auth/password";

    private readonly ApiWebApplicationFactory factory;
    private readonly InMemoryAuthStore store;

    public AuthenticationTests(ApiWebApplicationFactory factory)
    {
        this.factory = factory;
        store = factory.Store;

        // La factory est partagée par la classe : chaque test repart d'un état vierge.
        store.Reset();
    }

    private ITokenService Tokens => factory.Services.GetRequiredService<ITokenService>();

    // ---------- Le jeton doit être un access token ----------

    [Fact]
    public async Task A_refresh_token_cannot_be_used_as_an_access_token()
    {
        // Les deux jetons partagent secret, issuer et algorithme : seul le claim tokenType
        // les sépare. Sans ce contrôle, un refresh token volé vaudrait un accès de sept
        // jours, hors de la fenêtre de révocation de dix minutes.
        var refreshToken = Tokens.CreateRefreshToken(1);

        using var response = await Send(ProtectedRoute, refreshToken.Value);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_ACCESS_TOKEN", await ReadCode(response));
    }

    [Fact]
    public async Task A_disabled_account_cannot_use_its_access_token()
    {
        // Le claim isActive est gravé à l'émission : un compte désactivé après coup garde
        // un jeton signé valide, et seule cette vérification l'arrête.
        var token = AccessTokenFor(new AuthUser(2, "disabled@example.com", false, "HUMAN"), "READ_ONLY");

        using var response = await Send(ProtectedRoute, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_ACCESS_TOKEN", await ReadCode(response));
    }

    // ---------- Le jeton doit être authentique ----------

    [Fact]
    public async Task A_tampered_signature_is_refused()
    {
        var token = AccessTokenFor(new AuthUser(3, "user@example.com", true, "HUMAN"), "ADMIN");

        // Un attaquant qui s'octroie des droits doit resigner le jeton, ce qu'il ne peut
        // pas faire sans le secret. On altère ici le dernier caractère de la signature.
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        using var response = await Send(ProtectedRoute, tampered);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_ACCESS_TOKEN", await ReadCode(response));
    }

    [Fact]
    public async Task An_expired_access_token_is_refused()
    {
        store.AddUser(6, "user@example.com", withPasswordProvider: true);

        // ITokenService ne sait pas produire un jeton déjà expiré : il fixe toujours
        // notBefore à l'instant présent, et JwtSecurityToken refuse une expiration
        // antérieure au notBefore. Le jeton est donc forgé ici, entièrement dans le passé.
        var expired = ForgeAccessToken(6, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(-20));

        using var refused = await Send(ProtectedRoute, expired);

        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        Assert.Equal("INVALID_ACCESS_TOKEN", await ReadCode(refused));

        // Témoin indispensable : le même jeton forgé, mais valide dans le temps, doit être
        // accepté. Sans lui, une erreur de forge — mauvais algorithme, mauvais issuer, claim
        // oublié — ferait passer le refus ci-dessus pour la mauvaise raison.
        var stillValid = ForgeAccessToken(6, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10));

        using var accepted = await Send(ProtectedRoute, stillValid);

        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
    }

    [Fact]
    public async Task An_unreadable_token_is_refused()
    {
        using var response = await Send(ProtectedRoute, "not-a-jwt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_ACCESS_TOKEN", await ReadCode(response));
    }

    // ---------- Absence d'identité ----------

    [Fact]
    public async Task A_protected_route_is_refused_without_any_authorization_header()
    {
        using var request = ProtectedRequest(ProtectedRoute);
        using var response = await factory.CreateClient().SendAsync(request);

        // Aucun appelant identifié : le refus est une absence d'authentification, pas un
        // jeton invalide. Le front distingue les deux.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", await ReadCode(response));
    }

    [Fact]
    public async Task An_authorization_header_without_the_bearer_scheme_is_ignored()
    {
        var token = AccessTokenFor(new AuthUser(4, "user@example.com", true, "HUMAN"), "READ_ONLY");

        using var request = ProtectedRequest(ProtectedRoute);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", await ReadCode(response));
    }

    // ---------- Ce qui doit rester possible ----------

    [Fact]
    public async Task A_valid_access_token_is_accepted()
    {
        // Sans ce cas, tous les refus ci-dessus pourraient être verts pour une mauvaise
        // raison — une route cassée refuse tout le monde.
        store.AddUser(5, "user@example.com", withPasswordProvider: true);
        var token = AccessTokenFor(new AuthUser(5, "user@example.com", true, "HUMAN"), "READ_ONLY");

        using var response = await Send(ProtectedRoute, token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_public_route_stays_reachable_without_a_token()
    {
        // Garde-fou de l'authentification par défaut : les routes ouvertes doivent être
        // déclarées anonymes, et le rester.
        using var response = await factory.CreateClient().PostAsJsonAsync(
            "/auth/password/reset-request", new { email = "inconnu@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- L'authentification est exigée par défaut ----------

    [Fact]
    public async Task A_route_declaring_nothing_is_refused_without_a_token()
    {
        // `/_tests/unsecured` ne déclare aucune sécurité et n'appelle aucun use case
        // sécurisé : sans authentification par défaut, elle répond 200. C'est précisément ce
        // qu'on refuse — la protection ne doit pas dépendre du fait qu'on ait pensé à
        // l'écrire. Une route ajoutée demain doit être fermée sans qu'on ait rien fait.
        using var response = await factory.CreateClient().GetAsync("/_tests/unsecured");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", await ReadCode(response));
    }

    [Fact]
    public async Task A_route_declaring_nothing_is_reachable_with_a_valid_token()
    {
        // Contrepartie du test précédent : l'authentification par défaut ferme la route aux
        // anonymes, elle ne la ferme pas à tout le monde.
        var token = AccessTokenFor(new AuthUser(7, "user@example.com", true, "HUMAN"), "READ_ONLY");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/_tests/unsecured");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Utilitaires ----------

    private string AccessTokenFor(AuthUser user, string? role = null) =>
        Tokens.CreateAccessToken(user, role, new Dictionary<string, string>());

    // Reproduit un access token de production, en gardant la maîtrise de sa fenêtre de
    // validité. L'algorithme est celui que JwtTokenService déduit du secret de test
    // (39 octets, donc HS256) ; si ce secret change de taille, le témoin du test le révèle.
    private string ForgeAccessToken(long userId, DateTime notBefore, DateTime expires)
    {
        var issuer = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value.Issuer;
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(ApiWebApplicationFactory.TestJwtSecret));

        Claim[] claims =
        [
            new("tokenType", "ACCESS"),
            new("isActive", "true", ClaimValueTypes.Boolean),
            new("userType", "HUMAN"),
            new("role", "READ_ONLY"),
            new(JwtRegisteredClaimNames.Sub, userId.ToString())
        ];

        var token = new JwtSecurityToken(
            issuer,
            null,
            claims,
            notBefore,
            expires,
            new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Le corps est valide : la validation du modèle passe, et le refus vient donc bien de
    // l'authentification et non d'un 400.
    private static HttpRequestMessage ProtectedRequest(string route) =>
        new(HttpMethod.Patch, route)
        {
            Content = JsonContent.Create(new { password = "peu-importe" })
        };

    private async Task<HttpResponseMessage> Send(string route, string token)
    {
        using var request = ProtectedRequest(route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await factory.CreateClient().SendAsync(request);
    }

    private static async Task<string?> ReadCode(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return problem.GetProperty("code").GetString();
    }
}
