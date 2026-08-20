using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Users.Application.Dto;

namespace Tools.Api.Modules.Core.Users.Application.Usecases;

// Cas d'usage administrateur : lister les utilisateurs pour le tableau d'administration.
//
// Rôle minimum ADMIN, comme l'API Java. À noter que ADMIN est plus élevé que TECH dans la
// hiérarchie (voir RoleCode) : un compte TECH n'a donc pas accès à cette liste.
public sealed class ListUsersUseCase(
    UseCaseAuthorizer authorizer,
    IUserRepository userRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public Task<IReadOnlyList<UserAdminDto>> Execute()
    {
        return userRepository.FindAllForAdminAsync();
    }
}
