using Tools.Api.Modules.Access.Application.Dto;
using Tools.Api.Modules.Access.Application.Ports;
using Tools.Api.Modules.Security.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Application.Usecases;
using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Access.Application.Usecases;

// Cas d'usage administrateur : lister les modules fonctionnels.
//
// Rôle minimum ADMIN. Le contrôleur Java annote `@RequiredRole(TECH)`, mais cette annotation
// n'est lue par aucun aspect : c'est `requiredRole()` du use case — ADMIN — qui s'applique.
public sealed class ListModulesUseCase(
    UseCaseAuthorizer authorizer,
    IModuleRepository moduleRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public Task<IReadOnlyList<ModuleDto>> Execute()
    {
        return moduleRepository.FindAllAsync();
    }
}
