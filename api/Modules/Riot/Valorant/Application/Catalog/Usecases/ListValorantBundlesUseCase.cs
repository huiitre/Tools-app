using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;

public sealed class ListValorantBundlesUseCase(
    UseCaseAuthorizer authorizer,
    IValorantBundleRepository bundleRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public Task<List<ValorantBundleView>> Execute()
    {
        return bundleRepository.FindAll();
    }
}
