using Tools.ApiCore.Modules.Access.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Access.Application.Usecases;

// Cas d'usage administrateur : retirer l'accès d'un utilisateur à un module.
public sealed class RevokeModuleAccessUseCase(
    UseCaseAuthorizer authorizer,
    IModuleMembershipRepository membershipRepository,
    ITransactionManager transactionManager,
    ILogger<RevokeModuleAccessUseCase> logger
) : SecuredUseCase<RevokeModuleAccessCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override async Task Handle(RevokeModuleAccessCommand command, CurrentUser currentUser)
    {
        // Retirer un accès inexistant n'est pas anodin : c'est le signe que l'écran affiche
        // un état périmé. L'API Java rend la même erreur.
        if (!await membershipRepository.HasAccessAsync(command.ModuleId, command.UserId))
        {
            throw AppException.NotFound(
                "USER_MODULE_ROLE_NOT_FOUND",
                "Cet utilisateur n'a pas accès au module.");
        }

        await using var transaction = await transactionManager.BeginAsync();
        await membershipRepository.RevokeAsync(command.ModuleId, command.UserId);
        await transaction.CommitAsync();

        logger.LogInformation(
            "Accès module révoqué par userId={ActorId} : moduleId={ModuleId} cible={TargetUserId}",
            currentUser.UserId,
            command.ModuleId,
            command.UserId);
    }
}
