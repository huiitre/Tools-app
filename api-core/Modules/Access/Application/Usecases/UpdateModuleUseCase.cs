using Tools.ApiCore.Modules.Access.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Access.Application.Usecases;

// Cas d'usage administrateur : modifier un module fonctionnel, activation comprise.
public sealed class UpdateModuleUseCase(
    UseCaseAuthorizer authorizer,
    IModuleRepository moduleRepository
) : SecuredUseCase<UpdateModuleCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override async Task Handle(UpdateModuleCommand command, CurrentUser currentUser)
    {
        if (!await moduleRepository.ExistsAsync(command.ModuleId))
        {
            throw AppException.NotFound("MODULE_NOT_FOUND", "Module introuvable.");
        }

        var code = command.Code.Trim();

        // Le module qu'on modifie est exclu du contrôle : garder son propre code n'est pas
        // un conflit.
        if (await moduleRepository.CodeExistsAsync(code, command.ModuleId))
        {
            throw AppException.Conflict("MODULE_CODE_ALREADY_EXISTS", "Ce code de module est déjà utilisé.");
        }

        await moduleRepository.UpdateAsync(
            command.ModuleId,
            code,
            command.Name.Trim(),
            command.Description?.Trim(),
            command.Active);
    }
}
