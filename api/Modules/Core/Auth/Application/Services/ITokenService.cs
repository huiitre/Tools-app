using Tools.Api.Modules.Core.Auth.Domain;

namespace Tools.Api.Modules.Core.Auth.Application.Services;

// Port de gestion JWT : son implémentation connaît l'algorithme et le secret, pas les use cases.
public interface ITokenService
{
    // `role` est le rôle global de l'utilisateur, nul s'il n'en a aucun ; `modules` associe un
    // code module au rôle qu'il y détient. Les deux sont des valeurs uniques : la base n'en
    // autorise pas d'autre.
    string CreateAccessToken(
        AuthUser user,
        string? role,
        IReadOnlyDictionary<string, string> modules);
    IssuedToken CreateRefreshToken(long userId, DateTimeOffset? expiresAt = null);
    AccessTokenData ReadAccessToken(string token);
    RefreshTokenData ReadRefreshToken(string token);
}

// Un token émis conserve sa valeur et son expiration pour le cookie HTTP.
public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);

// Données fiables extraites d'un access token déjà vérifié. Seul l'identifiant en est tiré :
// l'autorisation d'une requête passe par les claims lus par HttpCurrentUserProvider, pas par
// ce type, qui ne sert qu'à convertir un token en session Electron.
public sealed record AccessTokenData(long UserId);

// Données fiables extraites d'un refresh token déjà vérifié.
public sealed record RefreshTokenData(long UserId, DateTimeOffset ExpiresAt);
