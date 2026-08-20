using Tools.Api.Modules.Core.Access.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Access.Application.Usecases;

// Cas d'usage administrateur : changer le rôle d'un membre à l'intérieur d'un module.
//
// Le rôle demandé remplace celui que l'utilisateur détenait sur ce module : (user_id,
// module_id) est la clé primaire de user_module_role, l'écriture est un upsert.
public sealed class ChangeModuleRoleUseCase(
    UseCaseAuthorizer authorizer,
    IModuleMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    ITransactionManager transactionManager,
    ILogger<ChangeModuleRoleUseCase> logger
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(ChangeModuleRoleCommand command)
    {
        // L'appartenance fait foi : on ne change pas le rôle de quelqu'un qui n'a pas accès
        // au module — ce serait le lui accorder par un chemin détourné.
        if (!await membershipRepository.HasAccessAsync(command.ModuleId, command.UserId))
        {
            throw AppException.NotFound(
                "USER_MODULE_ROLE_NOT_FOUND",
                "Cet utilisateur n'a pas accès au module.");
        }

        if (!await roleRepository.ExistsAsync(command.RoleId))
        {
            throw AppException.NotFound("ROLE_NOT_FOUND", "Rôle introuvable.");
        }

        await using var transaction = await transactionManager.BeginAsync();
        await membershipRepository.ChangeRoleAsync(command.ModuleId, command.UserId, command.RoleId);
        await transaction.CommitAsync();

        logger.LogInformation(
            "Rôle module modifié par userId={ActorId} : moduleId={ModuleId} cible={TargetUserId} roleId={RoleId}",
            CurrentUser.UserId,
            command.ModuleId,
            command.UserId,
            command.RoleId);
    }
}
