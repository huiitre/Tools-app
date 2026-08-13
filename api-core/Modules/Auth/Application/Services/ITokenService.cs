// Port de gestion JWT : son implémentation connaît l'algorithme et le secret, pas les use cases.
public interface ITokenService
{
    string CreateAccessToken(AuthUser user, IReadOnlyList<string> roles, IReadOnlyDictionary<string, string> modules);
    IssuedToken CreateRefreshToken(long userId, DateTimeOffset? expiresAt = null);
    RefreshTokenData ReadRefreshToken(string token);
}

// Un token émis conserve sa valeur et son expiration pour le cookie HTTP.
public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);

// Données fiables extraites d'un refresh token déjà vérifié.
public sealed record RefreshTokenData(long UserId, DateTimeOffset ExpiresAt);
