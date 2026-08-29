using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;

public sealed class GetValorantBundleByAssetIdUseCase(
    UseCaseAuthorizer authorizer,
    IValorantBundleRepository valorantBundleRepository
) : SecuredUseCase(authorizer)
{
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;

    public async Task<ValorantBundleView> Execute(Guid assetId)
    {
        var bundle = await valorantBundleRepository.FindByAssetId(assetId);

        return bundle ?? throw AppException.NotFound(
            "VALORANT_BUNDLE_NOT_FOUND",
            $"Le pack {assetId} est introuvable.");
    }
}