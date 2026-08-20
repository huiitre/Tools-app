using Tools.Api.Modules.Core.Access.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Access.Application.Usecases;

// Cas d'usage administrateur : retirer l'accès d'un utilisateur à un module.
public sealed class RevokeModuleAccessUseCase(
    UseCaseAuthorizer authorizer,
    IModuleMembershipRepository membershipRepository,
    ITransactionManager transactionManager,
    ILogger<RevokeModuleAccessUseCase> logger
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(RevokeModuleAccessCommand command)
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
            CurrentUser.UserId,
            command.ModuleId,
            command.UserId);
    }
}
