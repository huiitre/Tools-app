using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tools.Api.Modules.Auth.Application.Services;
using Tools.Api.Modules.Auth.Domain;
using Tools.Api.Modules.Common.Application.Exceptions;

namespace Tools.Api.Modules.Auth.Infrastructure.Jwt;

// Adaptateur JWT compatible avec l'API Java : même secret, issuer et choix d'algorithme HMAC.
public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    IConfiguration configuration,
    ILogger<JwtTokenService> logger) : ITokenService
{
    // IOptions lit appsettings ; IConfiguration lit ici la variable d'environnement JWT_SECRET.
    private readonly JwtOptions options = options.Value;
    private readonly string jwtSecret = configuration["JWT_SECRET"] ?? string.Empty;

    public string CreateAccessToken(
        AuthUser user,
        string? role,
        IReadOnlyDictionary<string, string> modules)
    {
        // Claims que l'API Java connaît déjà, puis rôles/droits pour la migration progressive.
        // "modules" porte un objet { code_module: code_rôle } : c'est ce claim qui permet de
        // décider d'un droit contextuel sans lire `tools_core.user_module_role`, et donc à un
        // service tiers de le faire sans accéder au schéma du Core (voir docs/SECURITY.md).
        var claims = new List<Claim>
        {
            new("tokenType", "ACCESS"),
            new("isActive", user.IsActive ? "true" : "false", ClaimValueTypes.Boolean),
            new("userType", user.UserType),
            new("modules", JsonSerializer.Serialize(modules), JsonClaimValueTypes.Json)
        };

        // Un compte sans rôle n'a pas de claim `role` du tout : une chaîne vide serait un code
        // à parser, donc une valeur à interpréter, là où l'absence ne s'interprète pas.
        if (role is not null)
        {
            claims.Add(new Claim("role", role));
        }

        // Un access token est volontairement court : 10 minutes par défaut.
        return Write(user.Id, claims, DateTimeOffset.UtcNow.AddSeconds(options.AccessTokenTtlSeconds));
    }

    public IssuedToken CreateRefreshToken(long userId, DateTimeOffset? expiresAt = null)
    {
        // Au premier login on crée une expiration ; au refresh on conserve celle du token initial.
        var tokenExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddSeconds(options.RefreshTokenTtlSeconds);
        return new IssuedToken(
            Write(userId, [new Claim("tokenType", "REFRESH")], tokenExpiresAt),
            tokenExpiresAt);
    }

    public AccessTokenData ReadAccessToken(string token)
    {
        try
        {
            var principal = Validate(token);
            if (principal.FindFirstValue("tokenType") != "ACCESS"
                || principal.FindFirstValue("isActive") != "true"
                || !long.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            {
                throw AppException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
            }

            return new AccessTokenData(userId);
        }
        catch (SecurityTokenException exception)
        {
            logger.LogDebug(exception, "Access JWT rejeté : {Reason}", exception.Message);
            throw AppException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
        }
        catch (ArgumentException exception)
        {
            logger.LogDebug(exception, "Access JWT illisible : {Reason}", exception.Message);
            throw AppException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
        }
    }

    public RefreshTokenData ReadRefreshToken(string token)
    {
        try
        {
            // Vérifie signature, issuer, durée de vie et algorithme avant toute lecture de claim.
            var principal = Validate(token, out var validatedToken);
            if (principal.FindFirstValue("tokenType") != "REFRESH"
                || !long.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
                || validatedToken is not JwtSecurityToken jwtToken)
            {
                throw AppException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
            }

            // Seules les données d'un token déjà validé remontent vers le use case.
            return new RefreshTokenData(userId, new DateTimeOffset(jwtToken.ValidTo, TimeSpan.Zero));
        }
        catch (SecurityTokenException exception)
        {
            logger.LogDebug(exception, "Refresh JWT rejeté : {Reason}", exception.Message);
            throw AppException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
        }
        catch (ArgumentException exception)
        {
            logger.LogDebug(exception, "Refresh JWT illisible : {Reason}", exception.Message);
            throw AppException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
        }
    }

    private System.Security.Claims.ClaimsPrincipal Validate(string token) => Validate(token, out _);

    private System.Security.Claims.ClaimsPrincipal Validate(string token, out SecurityToken validatedToken)
    {
        // Désactive le renommage automatique des claims .NET : on veut lire le "sub" JWT standard.
        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return tokenHandler.ValidateToken(token, ValidationParameters(), out validatedToken);
    }

    private string Write(long userId, IEnumerable<Claim> claims, DateTimeOffset expiresAt)
    {
        // "sub" est l'identifiant stable de l'utilisateur dans un JWT.
        var identity = new ClaimsIdentity(claims);
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));
        var token = new JwtSecurityToken(options.Issuer, null, identity.Claims, DateTime.UtcNow, expiresAt.UtcDateTime, SigningCredentials());
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Mêmes règles que celles appliquées par le middleware : une seule source les décrit.
    private TokenValidationParameters ValidationParameters() =>
        JwtTokenParameters.Validation(options, jwtSecret);

    // Signature et validation reposent sur la même clé et le même algorithme.
    private SigningCredentials SigningCredentials() => new(
        JwtTokenParameters.SigningKey(jwtSecret),
        JwtTokenParameters.Algorithm(jwtSecret));
}
