using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;
using Tools.ApiCore.Modules.Users.Application.Dto;

namespace Tools.ApiCore.Modules.Users.Application.Usecases;

// Cas d'usage administrateur : lister les utilisateurs pour le tableau d'administration.
//
// Rôle minimum ADMIN, comme l'API Java. À noter que ADMIN est plus élevé que TECH dans la
// hiérarchie (voir RoleCode) : un compte TECH n'a donc pas accès à cette liste.
public sealed class ListUsersUseCase(
    UseCaseAuthorizer authorizer,
    IUserRepository userRepository
) : SecuredQuery<IReadOnlyList<UserAdminDto>>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override Task<IReadOnlyList<UserAdminDto>> Handle(CurrentUser currentUser) =>
        userRepository.FindAllForAdminAsync();
}
