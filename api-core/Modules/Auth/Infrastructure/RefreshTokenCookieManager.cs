using Microsoft.Extensions.Options;

// Adaptateur HTTP responsable uniquement du cookie refresh token.
public sealed class RefreshTokenCookieManager(IOptions<JwtOptions> options, IHostEnvironment environment)
{
    private readonly JwtOptions options = options.Value;

    // HttpOnly interdit au JavaScript du front de lire ou modifier le refresh token.
    public void Set(HttpResponse response, string refreshToken, DateTimeOffset expiresAt) => response.Cookies.Append(
        options.RefreshCookieName,
        refreshToken,
        CookieOptions(expiresAt));

    // Le navigateur supprime un cookie dont l'expiration est forcée dans le passé.
    public void Clear(HttpResponse response) => response.Cookies.Delete(options.RefreshCookieName, CookieOptions(DateTimeOffset.UnixEpoch));

    public bool TryGet(HttpRequest request, out string refreshToken) => request.Cookies.TryGetValue(options.RefreshCookieName, out refreshToken!);

    private CookieOptions CookieOptions(DateTimeOffset expiresAt)
    {
        // En HTTPS (QA/prod), SameSite=None exige Secure. En dev HTTP, Secure doit rester false.
        var secure = !environment.IsDevelopment();
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
            Path = options.RefreshCookiePath,
            Expires = expiresAt
        };
    }
}
