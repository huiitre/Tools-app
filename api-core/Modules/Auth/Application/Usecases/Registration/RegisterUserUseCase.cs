using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Security.Cryptography;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;
using Tools.ApiCore.Modules.Auth.Application.Ports.Registration;
using Tools.ApiCore.Modules.Auth.Infrastructure.Registration;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Common.Infrastructure;
using Tools.ApiCore.Modules.Mail.Application;
using Tools.ApiCore.Modules.Mail.Application.Services;

namespace Tools.ApiCore.Modules.Auth.Application.Usecases.Registration;

// Cas d'usage visiteur : créer un compte par email et mot de passe.
//
// Ce use case n'hérite pas de SecuredUseCase, et c'est délibéré : seul un visiteur anonyme
// l'appelle. L'API Java le marque comme sécurisé, ce qui ne fonctionne que parce que son
// aspect laisse passer les appels non identifiés — ici, un tel marquage refuserait tout.
//
// Le compte est créé inactif et son email non vérifié : la connexion n'est possible qu'après
// avoir suivi le lien de confirmation.
public sealed class RegisterUserUseCase(
    IRegistrationRepository registrationRepository,
    IEmailVerificationRepository emailVerificationRepository,
    IPasswordHasher passwordHasher,
    ITransactionManager transactionManager,
    MailService mailService,
    IOptions<RegistrationOptions> registrationOptions,
    IOptions<AppOptions> appOptions,
    ILogger<RegisterUserUseCase> logger)
{
    private readonly RegistrationOptions options = registrationOptions.Value;

    public async Task Execute(RegisterUserCommand command)
    {
        var email = command.Email.Trim();
        var passwordHash = passwordHasher.Hash(command.Password);

        string token;
        await using (var transaction = await transactionManager.BeginAsync())
        {
            var existing = await registrationRepository.FindAccountByEmailAsync(email);

            long userId;
            if (existing is null)
            {
                userId = await registrationRepository.CreatePendingUserAsync(
                    command.Name.Trim(), email, passwordHash);
            }
            else if (existing.EmailVerifiedAt is null)
            {
                // Inscription reprise avant confirmation. Le mot de passe fourni cette fois
                // remplace le précédent : l'API Java le jetait en silence, et l'utilisateur
                // se retrouvait avec un mot de passe qu'il croyait avoir changé.
                userId = existing.Id;
                await registrationRepository.ReplacePendingPasswordAsync(userId, passwordHash);
                logger.LogDebug("Inscription reprise userId={UserId} : mot de passe remplacé.", userId);
            }
            else
            {
                // Adresse déjà confirmée : refus explicite. Le compte peut être suspendu
                // (is_active = false), cela ne rend pas l'adresse disponible pour autant.
                throw AppException.Conflict(
                    "EMAIL_ALREADY_REGISTERED",
                    "Un compte existe déjà avec cette adresse email.");
            }

            // Une seule demande active par utilisateur : la précédente est remplacée.
            await emailVerificationRepository.DeleteByUserIdAsync(userId);

            token = GenerateToken();
            await emailVerificationRepository.SaveAsync(
                userId, token, DateTime.UtcNow.AddMinutes(options.TokenTtlMinutes));

            await transaction.CommitAsync();
        }

        // L'email part après le commit : un jeton annulé ne peut jamais être envoyé.
        var link = $"{appOptions.Value.FrontendBaseUrl}{options.VerifyPath}?token={token}";
        await mailService.Send(
            new SendMailCommand(
                [email],
                "Confirmez votre adresse email",
                Text: $"""
                    Bonjour,

                    Votre compte a été créé. Pour l’activer, confirmez votre adresse email
                    en cliquant sur le lien suivant :

                    {link}

                    Ce lien expire dans {options.TokenTtlMinutes} minutes. Passé ce délai,
                    l’inscription est annulée et l’adresse redevient disponible.
                    """));

        logger.LogInformation("Email de confirmation envoyé pour une inscription.");
    }

    // Jeton aléatoire encodé en Base64 URL sans remplissage, comme le flux de réinitialisation.
    private string GenerateToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(options.TokenBytes));
}

public sealed record RegisterUserCommand(string Name, string Email, string Password);
