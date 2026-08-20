using Tools.Api.Modules.Core.Admin.Application.Dto;
using Tools.Api.Modules.Core.Admin.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Admin.Application.Usecases;

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
