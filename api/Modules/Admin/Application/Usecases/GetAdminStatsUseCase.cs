using Tools.Api.Modules.Admin.Application.Dto;
using Tools.Api.Modules.Admin.Application.Ports;
using Tools.Api.Modules.Security.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Application.Usecases;
using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Admin.Application.Usecases;

// Cas d'usage administrateur : indicateurs du tableau de bord.
public sealed class GetAdminStatsUseCase(
    UseCaseAuthorizer authorizer,
    IAdminStatsRepository adminStatsRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public Task<AdminStatsDto> Execute()
    {
        return adminStatsRepository.GetStatsAsync();
    }
}
