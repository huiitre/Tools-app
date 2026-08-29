using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;

public sealed class GetValorantBundleUseCase(
    UseCaseAuthorizer authorizer,
    IValorantBundleRepository bundleRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<ValorantBundleView> Execute(long id)
    {
        var bundle = await bundleRepository.FindById(id);

        return bundle ?? throw AppException.NotFound(
            "VALORANT_BUNDLE_NOT_FOUND",
            $"Le pack {id} est introuvable.");
    }
}
