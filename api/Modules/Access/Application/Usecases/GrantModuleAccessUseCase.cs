using Tools.ApiCore.Modules.Access.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;
using Tools.ApiCore.Modules.Users.Application;

namespace Tools.ApiCore.Modules.Access.Application.Usecases;

// Cas d'usage administrateur : ouvrir l'accès d'un utilisateur à un module.
//
// L'accès est accordé avec le rôle READ_ONLY, comme dans l'API Java : entrer dans un module
// ne confère aucun pouvoir. Le rôle se change ensuite explicitement.
public sealed class GrantModuleAccessUseCase(
    UseCaseAuthorizer authorizer,
    IModuleRepository moduleRepository,
    IModuleMembershipRepository membershipRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ITransactionManager transactionManager,
    ILogger<GrantModuleAccessUseCase> logger
) : SecuredUseCase(authorizer)
{
    private const string DefaultRoleCode = "READ_ONLY";

    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(GrantModuleAccessCommand command)
    {
        if (!await moduleRepository.ExistsAsync(command.ModuleId))
        {
            throw AppException.NotFound("MODULE_NOT_FOUND", "Module introuvable.");
        }

        if (!await userRepository.ExistsAsync(command.UserId))
        {
            throw AppException.NotFound("USER_NOT_FOUND", "Utilisateur introuvable.");
        }

        if (await membershipRepository.HasAccessAsync(command.ModuleId, command.UserId))
        {
            throw AppException.Conflict(
                "USER_ALREADY_HAS_ACCESS_TO_MODULE",
                "Cet utilisateur a déjà accès au module.");
        }

        var defaultRoleId = await roleRepository.FindIdByCodeAsync(DefaultRoleCode)
            ?? throw AppException.Unavailable(
                "DEFAULT_ROLE_NOT_FOUND",
                "Le rôle par défaut est introuvable.");

        await using var transaction = await transactionManager.BeginAsync();
        await membershipRepository.GrantAsync(command.ModuleId, command.UserId, defaultRoleId);
        await transaction.CommitAsync();

        logger.LogInformation(
            "Accès module accordé par userId={ActorId} : moduleId={ModuleId} cible={TargetUserId} rôle={RoleCode}",
            CurrentUser.UserId,
            command.ModuleId,
            command.UserId,
            DefaultRoleCode);
    }
}
