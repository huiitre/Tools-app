namespace Tools.ApiCore.Modules.Auth.Domain;

// Projection minimale de l'utilisateur nécessaire à l'authentification et aux claims JWT.
public sealed record AuthUser(long Id, string Email, bool IsActive, string UserType);
