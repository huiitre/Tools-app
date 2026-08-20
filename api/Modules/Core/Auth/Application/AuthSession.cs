namespace Tools.Api.Modules.Core.Auth.Application;

// Résultat interne commun aux cas Login et Refresh.
public sealed record AuthSession(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
