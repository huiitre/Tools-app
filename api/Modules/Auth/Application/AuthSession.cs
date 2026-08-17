namespace Tools.Api.Modules.Auth.Application;

// Résultat interne commun aux cas Login et Refresh.
public sealed record AuthSession(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
