// Adaptateur HTTP du port ICurrentUserProvider : lit l'access token de la requête.
// La validation (signature, issuer, expiration, type de token) est déléguée à
// ITokenService, qui lève déjà une erreur d'authentification si le token est invalide.
public sealed class HttpCurrentUserProvider(
    IHttpContextAccessor httpContextAccessor,
    ITokenService tokenService) : ICurrentUserProvider
{
    private const string BearerPrefix = "Bearer ";

    public CurrentUser? Current
    {
        get
        {
            // Hors requête HTTP (tâche de fond), il n'y a aucun utilisateur.
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            var header = httpContext.Request.Headers.Authorization.ToString();
            if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var token = header[BearerPrefix.Length..].Trim();
            if (token.Length == 0)
            {
                return null;
            }

            var accessToken = tokenService.ReadAccessToken(token);

            // Un code de rôle inconnu de l'énumération est ignoré plutôt que d'accorder un droit.
            var roles = accessToken.Roles
                .Select(RoleCodes.Parse)
                .Where(role => role is not null)
                .Select(role => role!.Value)
                .ToArray();

            return new CurrentUser(accessToken.UserId, roles);
        }
    }
}
