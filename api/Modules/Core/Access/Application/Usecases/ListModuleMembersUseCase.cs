using Tools.Api.Modules.Core.Access.Application.Dto;
using Tools.Api.Modules.Core.Access.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Access.Application.Usecases;

// Cas d'usage administrateur : lister les membres d'un module et leur rôle.
public sealed class ListModuleMembersUseCase(
    UseCaseAuthorizer authorizer,
    IModuleRepository moduleRepository,
    IModuleMembershipRepository membershipRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<IReadOnlyList<ModuleMemberDto>> Execute(long moduleId)
    {
        // Un module inexistant rend 404 plutôt qu'une liste vide, qui laisserait croire à un
        // module sans membre.
        if (!await moduleRepository.ExistsAsync(moduleId))
        {
            throw AppException.NotFound("MODULE_NOT_FOUND", "Module introuvable.");
        }

        return await membershipRepository.FindMembersAsync(moduleId);
    }
}
