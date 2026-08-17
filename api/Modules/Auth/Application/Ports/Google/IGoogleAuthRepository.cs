using Tools.Api.Modules.Auth.Domain;

namespace Tools.Api.Modules.Auth.Application.Ports.Google;

// Port BDD spécifique au rattachement d'une identité Google à un utilisateur Tools.
public interface IGoogleAuthRepository
{
    Task<AuthUser?> FindByGoogleProviderIdAsync(string providerUserId);
    Task<bool> ExistsByEmailAsync(string email);
    Task<AuthUser> CreateGoogleUserAsync(GoogleIdentity identity);
    Task UpdateGoogleAvatarAsync(long userId, string pictureUrl);
}
