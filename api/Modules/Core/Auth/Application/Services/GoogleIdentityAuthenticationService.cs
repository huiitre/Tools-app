using Tools.Api.Modules.Core.Auth.Domain;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;
using Tools.Api.Modules.Core.Settings.Application.Services;
using Tools.Api.Modules.Core.Settings.Domain;

namespace Tools.Api.Modules.Core.Auth.Application.Services;

// Service applicatif partagé par tous les flux Google : retrouve ou crée le compte Tools associé.
//
// C'est ici que passe la **seconde** porte d'entrée du site : le premier login Google crée le
// compte à la volée. Les paramètres d'inscription doivent donc s'y appliquer aussi, sans quoi
// fermer `/auth/register` laisserait la fenêtre Google grande ouverte.
public sealed class GoogleIdentityAuthenticationService(
    IGoogleAuthRepository googleAuthRepository,
    ITransactionManager transactionManager,
    SettingReader settings)
{
    // Le résultat distingue la connexion d'un compte connu de la toute première : seule la
    // seconde est une inscription, et elle seule doit être signalée aux administrateurs.
    public async Task<GoogleAuthenticationResult> AuthenticateAsync(GoogleIdentity identity)
    {
        // Les deux paramètres sont lus hors transaction, avec `GetGlobal` : personne n'est encore
        // identifié à ce stade du callback. Ils ne concernent que la **création** — un compte
        // existant se connecte quoi qu'il arrive, fermer les inscriptions ne met personne dehors.
        var registrationEnabled = await settings.GetGlobal(SettingCatalog.Auth.RegistrationEnabled);
        var approvalRequired = await settings.GetGlobal(SettingCatalog.Auth.AdminApprovalRequired);

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

        // Passé ce point on crée un compte : c'est ici, et pas plus haut, que les inscriptions
        // fermées se constatent. Le contrôle est délibérément après la recherche du compte
        // existant, pour qu'un habitué ne soit pas refusé au motif que la porte est close.
        if (!registrationEnabled)
        {
            throw AppException.Forbidden(
                "REGISTRATION_CLOSED",
                "Les inscriptions sont actuellement fermées.");
        }

        // Création de l'utilisateur, de son provider Google et de son rôle USER : une seule transaction.
        var user = await googleAuthRepository.CreateGoogleUserAsync(identity, activate: !approvalRequired);
        await transaction.CommitAsync();
        return new GoogleAuthenticationResult(user, AccountCreated: true);
    }
}

public sealed record GoogleAuthenticationResult(AuthUser User, bool AccountCreated);
