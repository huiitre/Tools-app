namespace Tools.Api.Modules.Core.Auth.Application.Ports.Password;

// Port du lien entre un utilisateur et ses méthodes d'authentification
// (tools_core.user_auth_provider).
public interface IUserAuthProviderRepository
{
    Task<bool> ExistsAsync(long userId, string provider);

    // Pour le provider PASSWORD, provider_user_id vaut l'email, comme à l'inscription.
    Task InsertAsync(
        long userId,
        string provider,
        string providerUserId,
        string? providerEmail);
}
