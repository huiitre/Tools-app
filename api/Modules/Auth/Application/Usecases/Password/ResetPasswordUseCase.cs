using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.Modules.Auth.Application.Usecases.Password;

// Cas d'usage utilisateur : définir un nouveau mot de passe à partir du lien reçu par email.
// L'identité vient du jeton : l'appelant n'est pas authentifié.
public sealed class ResetPasswordUseCase(
    IPasswordResetRepository passwordResetRepository,
    IUserCredentialsRepository userCredentialsRepository,
    IPasswordHasher passwordHasher,
    ITransactionManager transactionManager,
    ILogger<ResetPasswordUseCase> logger)
{
    public async Task Execute(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw AppException.Validation("INVALID_PASSWORD", "Le mot de passe est obligatoire.");
        }

        // La lecture du jeton, l'écriture du mot de passe et la suppression du jeton
        // forment une seule opération : un échec ne doit pas consommer le jeton.
        await using var transaction = await transactionManager.BeginAsync();

        var userId = await passwordResetRepository.FindUserIdByValidTokenAsync(token, DateTime.UtcNow)
            ?? throw AppException.Validation(
                "INVALID_PASSWORD_RESET_TOKEN",
                "Lien de réinitialisation du mot de passe invalide ou expiré.");

        var passwordHash = passwordHasher.Hash(newPassword);

        // Un compte disposant du provider PASSWORD sans ligne de credentials est anormal,
        // mais le mot de passe doit être écrit plutôt que perdu silencieusement.
        if (await userCredentialsRepository.UpdatePasswordAsync(userId, passwordHash) == 0)
        {
            await userCredentialsRepository.InsertAsync(userId, passwordHash);
        }

        await passwordResetRepository.DeleteByUserIdAsync(userId);
        await transaction.CommitAsync();

        logger.LogInformation("Mot de passe réinitialisé userId={UserId}", userId);
    }
}
