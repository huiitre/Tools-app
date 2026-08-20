using Tools.Api.Modules.Core.Security.Application.Dto;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Security.Application.Usecases;

// Cas d'usage administrateur : lister les rôles attribuables.
//
// Rôle minimum ADMIN, comme dans l'API Java. Son contrôleur annote `@RequiredRole(TECH)`,
// mais cette annotation n'est lue par aucun aspect : seul `SecuredUseCase.requiredRole()`
// est appliqué, et il vaut ADMIN. C'est le comportement réel qui est reproduit ici.
public sealed class ListRolesUseCase(
    UseCaseAuthorizer authorizer,
    IRoleRepository roleRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public Task<IReadOnlyList<RoleDto>> Execute()
    {
        return roleRepository.FindAllAsync();
    }
}
