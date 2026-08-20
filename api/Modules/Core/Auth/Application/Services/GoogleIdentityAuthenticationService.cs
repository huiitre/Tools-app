using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Domain;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;

namespace Tools.Api.Modules.Core.Auth.Application.Services;

// Service applicatif partagé par tous les flux Google : retrouve ou crée le compte Tools associé.
public sealed class GoogleIdentityAuthenticationService(
    IGoogleAuthRepository googleAuthRepository,
    ITransactionManager transactionManager)
{
    // Le résultat distingue la connexion d'un compte connu de la toute première : seule la
    // seconde est une inscription, et elle seule doit être signalée aux administrateurs.
    public async Task<GoogleAuthenticationResult> AuthenticateAsync(GoogleIdentity identity)
    {
        // La recherche, l'éventuelle mise à jour d'avatar et la création sont cohérentes dans une transaction.
        await using var transaction = await transactionManager.BeginAsync();
        var existingUser = await googleAuthRepository.FindByGoogleProviderIdAsync(identity.ProviderUserId);
        if (existingUser is not null)
        {
            if (!existingUser.IsActive)
            {
                throw AppException.Unauthorized("USER_DISABLED", "Utilisateur désactivé.");
            }

            if (!string.IsNullOrWhiteSpace(identity.PictureUrl))
            {
                await googleAuthRepository.UpdateGoogleAvatarAsync(existingUser.Id, identity.PictureUrl);
            }

            await transaction.CommitAsync();
            return new GoogleAuthenticationResult(existingUser, AccountCreated: false);
        }

        if (await googleAuthRepository.ExistsByEmailAsync(identity.Email))
        {
            throw AppException.Conflict(
                "GOOGLE_EMAIL_ALREADY_REGISTERED",
                "Un compte existe déjà avec cette adresse email.");
        }

        // Création de l'utilisateur, de son provider Google et de son rôle USER : une seule transaction.
        var user = await googleAuthRepository.CreateGoogleUserAsync(identity);
        await transaction.CommitAsync();
        return new GoogleAuthenticationResult(user, AccountCreated: true);
    }
}

public sealed record GoogleAuthenticationResult(AuthUser User, bool AccountCreated);
