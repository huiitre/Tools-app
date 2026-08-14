using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Domain;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Auth.Application.Ports.Google;

namespace Tools.ApiCore.Modules.Auth.Application.Services;

// Service applicatif partagé par tous les flux Google : retrouve ou crée le compte Tools associé.
public sealed class GoogleIdentityAuthenticationService(
    IGoogleAuthRepository googleAuthRepository,
    ITransactionManager transactionManager)
{
    public async Task<AuthUser> AuthenticateAsync(GoogleIdentity identity, CancellationToken cancellationToken)
    {
        // La recherche, l'éventuelle mise à jour d'avatar et la création sont cohérentes dans une transaction.
        await using var transaction = await transactionManager.BeginAsync();
        var existingUser = await googleAuthRepository.FindByGoogleProviderIdAsync(identity.ProviderUserId, cancellationToken);
        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
            {
                throw AppException.Unauthorized("USER_DISABLED", "Utilisateur désactivé.");
            }

            if (!string.IsNullOrWhiteSpace(identity.PictureUrl))
            {
                await googleAuthRepository.UpdateGoogleAvatarAsync(existingUser.Id, identity.PictureUrl, cancellationToken);
            }

            await transaction.CommitAsync();
            return existingUser;
        }

        if (await googleAuthRepository.ExistsByEmailAsync(identity.Email, cancellationToken))
        {
            throw AppException.Conflict(
                "GOOGLE_EMAIL_ALREADY_REGISTERED",
                "Un compte existe déjà avec cette adresse email.");
        }

        // Création de l'utilisateur, de son provider Google et de son rôle USER : une seule transaction.
        var user = await googleAuthRepository.CreateGoogleUserAsync(identity, cancellationToken);
        await transaction.CommitAsync();
        return user;
    }
}
