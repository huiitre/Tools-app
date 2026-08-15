using Tools.ApiCore.Modules.Access.Application.Dto;
using Tools.ApiCore.Modules.Access.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Access.Application.Usecases;

// Cas d'usage administrateur : lister les modules fonctionnels.
//
// Rôle minimum ADMIN. Le contrôleur Java annote `@RequiredRole(TECH)`, mais cette annotation
// n'est lue par aucun aspect : c'est `requiredRole()` du use case — ADMIN — qui s'applique.
public sealed class ListModulesUseCase(
    UseCaseAuthorizer authorizer,
    IModuleRepository moduleRepository
) : SecuredQuery<IReadOnlyList<ModuleDto>>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override Task<IReadOnlyList<ModuleDto>> Handle(CurrentUser currentUser) =>
        moduleRepository.FindAllAsync();
}
