using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.Modules.Auth.Application.Usecases.Password;

// Cas d'usage utilisateur : définir ou changer son mot de passe depuis ses options.
//
// Accessible à tout utilisateur authentifié : READ_ONLY est le rôle le plus bas,
// donc chacun peut agir sur son propre compte, jamais sur celui d'un autre puisque
// l'identifiant vient du jeton et non de la requête.
//
// Un compte Google qui n'a pas encore de mot de passe obtient à cette occasion sa
// ligne de credentials et son provider PASSWORD : il pourra ensuite se connecter par
// mot de passe et utiliser « mot de passe oublié ».
public sealed class SetUserPasswordUseCase(
    UseCaseAuthorizer authorizer,
    IAuthRepository authRepository,
    IUserCredentialsRepository userCredentialsRepository,
    IUserAuthProviderRepository userAuthProviderRepository,
    IPasswordHasher passwordHasher,
    ITransactionManager transactionManager,
    ILogger<SetUserPasswordUseCase> logger) : SecuredUseCase<SetUserPasswordCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;

    protected override async Task Handle(
        SetUserPasswordCommand command,
        CurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw AppException.Validation("INVALID_PASSWORD", "Le mot de passe est obligatoire.");
        }

        var user = await authRepository.FindByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw AppException.NotFound("USER_NOT_FOUND", "Utilisateur introuvable.");

        var passwordHash = passwordHasher.Hash(command.Password);

        // Credentials et provider doivent apparaître ensemble ou pas du tout.
        await using var transaction = await transactionManager.BeginAsync();

        if (await userCredentialsRepository.ExistsAsync(user.Id, cancellationToken))
        {
            await userCredentialsRepository.UpdatePasswordAsync(user.Id, passwordHash, cancellationToken);
        }
        else
        {
            await userCredentialsRepository.InsertAsync(user.Id, passwordHash, cancellationToken);

            // provider_user_id vaut l'email, même convention qu'à l'inscription.
            if (!await userAuthProviderRepository.ExistsAsync(user.Id, "PASSWORD", cancellationToken))
            {
                await userAuthProviderRepository.InsertAsync(
                    user.Id, "PASSWORD", user.Email, user.Email, cancellationToken);

                logger.LogInformation("Provider PASSWORD créé userId={UserId}", user.Id);
            }
        }

        await transaction.CommitAsync();
        logger.LogInformation("Mot de passe défini userId={UserId}", user.Id);
    }
}

public sealed record SetUserPasswordCommand(string Password);
