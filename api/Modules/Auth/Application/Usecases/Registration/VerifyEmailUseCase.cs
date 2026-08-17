using Tools.Api.Modules.Auth.Application.Ports.Registration;
using Tools.Api.Modules.Auth.Application.Services;
using Tools.Api.Modules.Common.Application.Exceptions;
using Tools.Api.Modules.Common.Application.Ports;

namespace Tools.Api.Modules.Auth.Application.Usecases.Registration;

// Cas d'usage visiteur : confirmer son adresse email et activer son compte.
//
// Non sécurisé, comme l'inscription : l'appelant n'a pas encore de session, c'est
// justement ce que ce flux lui permet d'obtenir.
public sealed class VerifyEmailUseCase(
    IEmailVerificationRepository emailVerificationRepository,
    IRegistrationRepository registrationRepository,
    ITransactionManager transactionManager,
    AdminSignupNotifier adminSignupNotifier,
    ILogger<VerifyEmailUseCase> logger)
{
    public async Task Execute(string token)
    {
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

        // La confirmation active le compte et marque l'adresse comme vérifiée. Les deux vont
        // ensemble à l'inscription, mais restent deux informations distinctes ensuite : un
        // administrateur peut suspendre le compte sans que l'adresse cesse d'être confirmée.
        await registrationRepository.MarkEmailVerifiedAsync(userId.Value, DateTime.UtcNow);

        // Le jeton est consommé : le lien ne peut pas resservir.
        await emailVerificationRepository.DeleteByUserIdAsync(userId.Value);

        // Lue avant le commit, tant que la transaction porte encore la connexion.
        var email = await registrationRepository.FindEmailByIdAsync(userId.Value);

        await transaction.CommitAsync();

        logger.LogInformation("Adresse email confirmée userId={UserId}", userId.Value);

        if (email is not null)
        {
            await adminSignupNotifier.EmailVerified(email);
        }
    }
}
