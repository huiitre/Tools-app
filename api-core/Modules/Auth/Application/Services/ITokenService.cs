// Port de gestion JWT : son implémentation connaît l'algorithme et le secret, pas les use cases.
public interface ITokenService
{
    string CreateAccessToken(AuthUser user, IReadOnlyList<string> roles, IReadOnlyDictionary<string, string> modules);
    IssuedToken CreateRefreshToken(long userId, DateTimeOffset? expiresAt = null);
    AccessTokenData ReadAccessToken(string token);
    RefreshTokenData ReadRefreshToken(string token);
}

// Un token émis conserve sa valeur et son expiration pour le cookie HTTP.
public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);

// Données fiables extraites d'un access token déjà vérifié.
// Les rôles sont ceux gravés à l'émission : ils font autorité tant que le token est valide.
public sealed record AccessTokenData(long UserId, IReadOnlyList<string> Roles);

// Données fiables extraites d'un refresh token déjà vérifié.
public sealed record RefreshTokenData(long UserId, DateTimeOffset ExpiresAt);
