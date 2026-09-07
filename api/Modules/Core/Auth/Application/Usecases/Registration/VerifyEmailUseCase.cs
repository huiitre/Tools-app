using Tools.Api.Modules.Core.Auth.Application.Ports.Registration;
using Tools.Api.Modules.Core.Auth.Application.Services;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Settings.Application.Services;
using Tools.Api.Modules.Core.Settings.Domain;

namespace Tools.Api.Modules.Core.Auth.Application.Usecases.Registration;

// Cas d'usage visiteur : confirmer son adresse email et activer son compte.
//
// Non sécurisé, comme l'inscription : l'appelant n'a pas encore de session, c'est
// justement ce que ce flux lui permet d'obtenir.
public sealed class VerifyEmailUseCase(
    IEmailVerificationRepository emailVerificationRepository,
    IRegistrationRepository registrationRepository,
    ITransactionManager transactionManager,
    AdminSignupNotifier adminSignupNotifier,
    SettingReader settings,
    ILogger<VerifyEmailUseCase> logger)
{
    // Rend l'état où se trouve le compte à l'issue de la confirmation. Sans ça la route ne
    // pouvait répondre que 204, et le frontend n'avait aucun moyen de distinguer « tu peux te
    // connecter » de « attends qu'un administrateur t'active » — il affichait donc le premier
    // message dans les deux cas.
    public async Task<VerifyEmailResult> Execute(string token)
    {
        // Lu hors transaction — le paramètre vit dans une autre table et n'a rien à y faire —
        // et avec `GetGlobal` : celui qui suit le lien de confirmation n'a pas encore de session.
        var approvalRequired = await settings.GetGlobal(SettingCatalog.Auth.AdminApprovalRequired);

        await using var transaction = await transactionManager.BeginAsync();

        var userId = await emailVerificationRepository.FindUserIdByValidTokenAsync(token, DateTime.UtcNow);
        if (userId is null)
        {
            // Même réponse pour un jeton inconnu, déjà consommé ou expiré : rien ne permet
            // de distinguer les trois de l'extérieur.
            logger.LogDebug("Confirmation refusée : jeton inconnu ou expiré.");
            throw AppException.Validation(
                "INVALID_EMAIL_VERIFICATION_TOKEN",
                "Ce lien de confirmation est invalide ou expiré.");
        }

        // La confirmation marque toujours l'adresse comme vérifiée ; elle n'active le compte que
        // si aucune validation d'administrateur n'est exigée. Les deux informations restent
        // distinctes : un administrateur peut suspendre le compte sans que l'adresse cesse
        // d'être confirmée, et un compte en attente a bien une adresse confirmée.
        await registrationRepository.MarkEmailVerifiedAsync(
            userId.Value, DateTime.UtcNow, activate: !approvalRequired);

        // Le jeton est consommé : le lien ne peut pas resservir.
        await emailVerificationRepository.DeleteByUserIdAsync(userId.Value);

        // Lue avant le commit, tant que la transaction porte encore la connexion.
        var email = await registrationRepository.FindEmailByIdAsync(userId.Value);

        await transaction.CommitAsync();

        logger.LogInformation(
            "Adresse email confirmée userId={UserId} compteActif={Active}",
            userId.Value,
            !approvalRequired);

        if (email is not null)
        {
            await adminSignupNotifier.EmailVerified(email, approvalRequired);
        }

        return approvalRequired ? VerifyEmailResult.PendingApproval : VerifyEmailResult.Active;
    }
}

// Ce que le visiteur doit lire après avoir suivi son lien. Une énumération plutôt qu'un booléen :
// l'appelant écrit `VerifyEmailResult.PendingApproval`, pas `true`, dont le sens se devine.
public enum VerifyEmailResult
{
    // Adresse confirmée, compte actif : la connexion est ouverte.
    Active,

    // Adresse confirmée, compte laissé inactif : un administrateur doit l'activer.
    PendingApproval
}
