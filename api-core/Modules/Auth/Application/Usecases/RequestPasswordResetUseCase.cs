using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Security.Cryptography;

// Cas d'usage utilisateur : demander un lien de réinitialisation de mot de passe.
//
// Le use case ne renvoie jamais d'information sur l'existence du compte : quel que soit
// le cas, l'appelant reçoit la même réponse. Seul un compte disposant du provider
// PASSWORD reçoit un email ; un compte Google qui n'a pas encore défini de mot de passe
// doit d'abord en créer un depuis ses options.
public sealed class RequestPasswordResetUseCase(
    IAuthRepository authRepository,
    IUserAuthProviderRepository userAuthProviderRepository,
    IPasswordResetRepository passwordResetRepository,
    ITransactionManager transactionManager,
    MailService mailService,
    IOptions<PasswordResetOptions> passwordResetOptions,
    IOptions<AppOptions> appOptions,
    ILogger<RequestPasswordResetUseCase> logger)
{
    private readonly PasswordResetOptions options = passwordResetOptions.Value;

    public async Task Execute(string email, CancellationToken cancellationToken)
    {
        var user = await authRepository.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            logger.LogDebug("Demande de réinitialisation pour un email inconnu.");
            return;
        }

        string token;
        await using (var transaction = await transactionManager.BeginAsync())
        {
            // Sans provider PASSWORD, il n'y a pas de mot de passe à réinitialiser.
            if (!await userAuthProviderRepository.ExistsAsync(user.Id, "PASSWORD", cancellationToken))
            {
                logger.LogDebug("Demande de réinitialisation refusée userId={UserId} : aucun provider PASSWORD.", user.Id);
                return;
            }

            // Une seule demande active par utilisateur : la précédente est remplacée.
            await passwordResetRepository.DeleteByUserIdAsync(user.Id, cancellationToken);

            token = GenerateToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(options.TokenTtlMinutes);
            await passwordResetRepository.SaveAsync(user.Id, token, expiresAt, cancellationToken);

            await transaction.CommitAsync();
        }

        // L'email part après le commit : un jeton annulé ne peut jamais être envoyé.
        var link = $"{appOptions.Value.FrontendBaseUrl}{options.ResetPath}?token={token}";
        await mailService.Send(
            new SendMailCommand(
                [user.Email],
                "Réinitialisation de votre mot de passe",
                Text: $"""
                    Bonjour,

                    Une demande de réinitialisation de mot de passe a été effectuée.
                    Pour définir un nouveau mot de passe, cliquez sur le lien suivant :

                    {link}

                    Ce lien expire dans {options.TokenTtlMinutes} minutes.
                    Si vous n’êtes pas à l’origine de cette demande, ignorez cet email.
                    """),
            cancellationToken);

        logger.LogInformation("Email de réinitialisation envoyé userId={UserId}", user.Id);
    }

    // Jeton aléatoire encodé en Base64 URL sans remplissage, comme côté Java.
    private string GenerateToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(options.TokenBytes));
}
