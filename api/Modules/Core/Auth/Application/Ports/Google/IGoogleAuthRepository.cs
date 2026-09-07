using Tools.Api.Modules.Core.Auth.Domain;

namespace Tools.Api.Modules.Core.Auth.Application.Ports.Google;

// Port BDD spécifique au rattachement d'une identité Google à un utilisateur Tools.
public interface IGoogleAuthRepository
{
    Task<AuthUser?> FindByGoogleProviderIdAsync(string providerUserId);
    Task<bool> ExistsByEmailAsync(string email);
    // `activate` est faux quand `auth.adminApprovalRequired` est posé. Google a bien confirmé
    // l'adresse, mais le compte attend qu'un administrateur l'active — comme une inscription
    // par mot de passe qui vient d'être confirmée.
    Task<AuthUser> CreateGoogleUserAsync(GoogleIdentity identity, bool activate);
    Task UpdateGoogleAvatarAsync(long userId, string pictureUrl);
}
