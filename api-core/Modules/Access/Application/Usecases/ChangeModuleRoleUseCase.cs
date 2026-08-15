using Tools.ApiCore.Modules.Access.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Access.Application.Usecases;

// Cas d'usage administrateur : changer le rôle d'un membre à l'intérieur d'un module.
//
// Le rôle demandé remplace tout ce que l'utilisateur détenait sur ce module. La table
// autorise le cumul, mais ni le frontend ni ce use case ne l'expriment.
public sealed class ChangeModuleRoleUseCase(
    UseCaseAuthorizer authorizer,
    IModuleMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    ITransactionManager transactionManager
) : SecuredUseCase<ChangeModuleRoleCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override async Task Handle(ChangeModuleRoleCommand command, CurrentUser currentUser)
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
    }
}
