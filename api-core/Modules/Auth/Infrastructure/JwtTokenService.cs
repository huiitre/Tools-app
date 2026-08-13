using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// Paramètres non sensibles versionnés. Le secret vient uniquement de JWT_SECRET.
public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";
    public string Issuer { get; init; } = "tools-api";
    public int AccessTokenTtlSeconds { get; init; } = 600;
    public int RefreshTokenTtlSeconds { get; init; } = 604800;
    public string RefreshCookieName { get; init; } = "refresh_token";
    public string RefreshCookiePath { get; init; } = "/";
}

// Adaptateur JWT compatible avec l'API Java : même secret, issuer et choix d'algorithme HMAC.
public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    IConfiguration configuration,
    ILogger<JwtTokenService> logger) : ITokenService
{
    // IOptions lit appsettings ; IConfiguration lit ici la variable d'environnement JWT_SECRET.
    private readonly JwtOptions options = options.Value;
    private readonly string jwtSecret = configuration["JWT_SECRET"] ?? string.Empty;

    public string CreateAccessToken(AuthUser user, IReadOnlyList<string> roles, IReadOnlyDictionary<string, string> modules)
    {
        // Claims que l'API Java connaît déjà, puis rôles/droits pour la migration progressive.
        var claims = new List<Claim>
        {
            new("tokenType", "ACCESS"),
            new("isActive", user.IsActive ? "true" : "false", ClaimValueTypes.Boolean),
            new("userType", user.UserType),
            new("roles", JsonSerializer.Serialize(roles), JsonClaimValueTypes.JsonArray),
            new("modules", JsonSerializer.Serialize(modules), JsonClaimValueTypes.Json)
        };

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
                throw ApplicationException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
            }

            return new AccessTokenData(userId);
        }
        catch (SecurityTokenException exception)
        {
            logger.LogDebug(exception, "Access JWT rejeté : {Reason}", exception.Message);
            throw ApplicationException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
        }
        catch (ArgumentException exception)
        {
            logger.LogDebug(exception, "Access JWT illisible : {Reason}", exception.Message);
            throw ApplicationException.Unauthorized("INVALID_ACCESS_TOKEN", "Session invalide ou expirée.");
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
                throw ApplicationException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
            }

            // Seules les données d'un token déjà validé remontent vers le use case.
            return new RefreshTokenData(userId, new DateTimeOffset(jwtToken.ValidTo, TimeSpan.Zero));
        }
        catch (SecurityTokenException exception)
        {
            logger.LogDebug(exception, "Refresh JWT rejeté : {Reason}", exception.Message);
            throw ApplicationException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
        }
        catch (ArgumentException exception)
        {
            logger.LogDebug(exception, "Refresh JWT illisible : {Reason}", exception.Message);
            throw ApplicationException.Unauthorized("INVALID_REFRESH_TOKEN", "Session invalide ou expirée.");
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

    // Règles appliquées systématiquement avant d'accepter un refresh token.
    private TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey(),
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidAlgorithms = [SigningAlgorithm()]
    };

    // Signature et validation reposent sur la même clé et le même algorithme.
    private SigningCredentials SigningCredentials() => new(SigningKey(), SigningAlgorithm());

    private SymmetricSecurityKey SigningKey() => new(SecretBytes());

    // JJWT choisit l'algorithme HMAC selon la taille de la clé ; on reproduit ce comportement pour Java/Core.
    private string SigningAlgorithm() => SecretBytes().Length switch
    {
        >= 64 => SecurityAlgorithms.HmacSha512,
        >= 48 => SecurityAlgorithms.HmacSha384,
        _ => SecurityAlgorithms.HmacSha256
    };

    private byte[] SecretBytes()
    {
        // HS256 requiert au minimum 32 octets ; une clé insuffisante est une erreur de configuration.
        var secretBytes = Encoding.UTF8.GetBytes(jwtSecret);
        return secretBytes.Length >= 32
            ? secretBytes
            : throw new InvalidOperationException("JWT_SECRET doit contenir au moins 32 octets UTF-8.");
    }
}
