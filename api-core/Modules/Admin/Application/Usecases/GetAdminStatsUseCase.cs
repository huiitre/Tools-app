using Tools.ApiCore.Modules.Admin.Application.Dto;
using Tools.ApiCore.Modules.Admin.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Admin.Application.Usecases;

// Cas d'usage administrateur : indicateurs du tableau de bord.
public sealed class GetAdminStatsUseCase(
    UseCaseAuthorizer authorizer,
    IAdminStatsRepository adminStatsRepository
) : SecuredQuery<AdminStatsDto>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override Task<AdminStatsDto> Handle(CurrentUser currentUser) =>
        adminStatsRepository.GetStatsAsync();
}
