using Tools.Api.Modules.Auth.Application.Ports;
using Tools.Api.Modules.Common.Application.Ports;
using Tools.Api.Modules.Security.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Application.Usecases;
using Tools.Api.Modules.Security.Domain;
using Tools.Api.Modules.Common.Application.Exceptions;
using Tools.Api.Modules.Auth.Application.Ports.Password;

namespace Tools.Api.Modules.Auth.Application.Usecases.Password;

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
    ILogger<SetUserPasswordUseCase> logger) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;

    public async Task Execute(SetUserPasswordCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw AppException.Validation("INVALID_PASSWORD", "Le mot de passe est obligatoire.");
        }

        var user = await authRepository.FindByIdAsync(CurrentUser.UserId)
            ?? throw AppException.NotFound("USER_NOT_FOUND", "Utilisateur introuvable.");

        var passwordHash = passwordHasher.Hash(command.Password);

        // Credentials et provider doivent apparaître ensemble ou pas du tout.
        await using var transaction = await transactionManager.BeginAsync();

        if (await userCredentialsRepository.ExistsAsync(user.Id))
        {
            await userCredentialsRepository.UpdatePasswordAsync(user.Id, passwordHash);
        }
        else
        {
            await userCredentialsRepository.InsertAsync(user.Id, passwordHash);

            // provider_user_id vaut l'email, même convention qu'à l'inscription.
            if (!await userAuthProviderRepository.ExistsAsync(user.Id, "PASSWORD"))
            {
                await userAuthProviderRepository.InsertAsync(
                    user.Id, "PASSWORD", user.Email, user.Email);

                logger.LogInformation("Provider PASSWORD créé userId={UserId}", user.Id);
            }
        }

        await transaction.CommitAsync();
        logger.LogInformation("Mot de passe défini userId={UserId}", user.Id);
    }
}

public sealed record SetUserPasswordCommand(string Password);
