namespace Tools.Api.Modules.Core.Auth.Infrastructure.Jwt;

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
