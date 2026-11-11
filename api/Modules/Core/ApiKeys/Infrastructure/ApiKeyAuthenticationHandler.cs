using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.ApiKeys.Application.Services;
using Tools.Api.Modules.Core.Common.Api.Errors;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Security.Infrastructure;

namespace Tools.Api.Modules.Core.ApiKeys.Infrastructure;

// « Cette clé est-elle valide, et quels scopes porte-t-elle ? ». Comme le JWT, ne décide d'aucun
// droit : le scope exigé est vérifié par le use case.
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    ApiKeyAuthenticator authenticator,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    private const string LegacyTokenConfigurationKey = "INTERNAL_API_TOKEN";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var method = Request.Method;
        var path = Request.Path.Value ?? "";

        var presentedKey = Request.Headers[ApiKeyAuthenticationDefaults.HeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(presentedKey))
        {
            var apiKey = await authenticator.Authenticate(
                presentedKey.Trim(), Context.Connection.RemoteIpAddress, method, path);

            if (apiKey is null)
            {
                return AuthenticateResult.Fail("Clé d'API inconnue, révoquée ou expirée.");
            }

            var claims = new List<Claim>
            {
                new(ApiKeyClaims.Id, apiKey.Id!.Value.ToString()),
                new(ApiKeyClaims.Name, apiKey.Name)
            };
            claims.AddRange(apiKey.Scopes.Select(scope => new Claim(ApiKeyClaims.Scope, scope.ToCode())));

            LogWhenResponseStarts(apiKey, method, path);
            return Success(claims);
        }

        var legacyToken = Request.Headers[ApiKeyAuthenticationDefaults.LegacyHeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(legacyToken))
        {
            if (!LegacyTokenMatches(legacyToken))
            {
                await authenticator.LogLegacyRejected(method, path);
                return AuthenticateResult.Fail("Secret partagé invalide.");
            }

            // Même pouvoir qu'avant la migration : toutes les routes qui acceptent une clé.
            var claims = new List<Claim> { new(ApiKeyClaims.Name, ApiKeyAuthenticationDefaults.LegacyHeaderName) };
            claims.AddRange(ApiKeyScopes.AllCodes.Select(code => new Claim(ApiKeyClaims.Scope, code)));

            LogWhenResponseStarts(null, method, path);
            return Success(claims);
        }

        return AuthenticateResult.NoResult();
    }

    // Une seule réponse pour tous les refus : ne pas dire à qui cherche si la clé existe.
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var problem = Context.RequestServices
            .GetRequiredService<ApiProblemDetailsFactory>()
            .Create(Context, StatusCodes.Status401Unauthorized, "INVALID_API_KEY", "Unauthorized",
                "Clé d'API invalide, révoquée ou expirée.");

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/problem+json";
        Response.Headers["X-Request-Id"] = Context.TraceIdentifier;
        await JsonSerializer.SerializeAsync(
            Response.Body, problem, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    // Au départ de la réponse, le code final est connu, refus du use case compris. L'authentification
    // pouvant être rejouée dans la même requête, une seule ligne est écrite.
    private void LogWhenResponseStarts(Domain.ApiKey? apiKey, string method, string path)
    {
        if (!Context.Items.TryAdd(typeof(ApiKeyAuthenticationHandler), true))
        {
            return;
        }

        Response.OnStarting(() => authenticator.LogUse(apiKey, method, path, Response.StatusCode));
    }

    private AuthenticateResult Success(IEnumerable<Claim> claims)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    // Secret absent de la configuration : refusé, jamais ouvert. Comparaison à temps constant.
    private bool LegacyTokenMatches(string presented)
    {
        var expected = configuration[LegacyTokenConfigurationKey];
        return !string.IsNullOrWhiteSpace(expected)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(expected));
    }
}
