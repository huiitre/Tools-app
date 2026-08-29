using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Catalogue des packs.
[ApiController]
[Route("riot/valorant")]
public class ValorantBundleController : ControllerBase
{
    [HttpGet("bundles")]
    public Task<List<ValorantBundleView>> ListBundles(
        [FromServices] ListValorantBundlesUseCase listValorantBundlesUseCase
    )
    {
        return listValorantBundlesUseCase.Execute();
    }

    [HttpGet("bundles/{id:long}")]
    public Task<ValorantBundleView> GetBundle(
        [FromServices] GetValorantBundleUseCase getValorantBundleUseCase,
        [FromRoute] long id
    )
    {
        return getValorantBundleUseCase.Execute(id);
    }

    [HttpGet("bundles/by-asset/{assetId:guid}")]
    public Task<ValorantBundleView> GetBundleByAssetId(
        [FromServices] GetValorantBundleByAssetIdUseCase getValorantBundleByAssetIdUseCase,
        [FromRoute] Guid assetId
    )
    {
        return getValorantBundleByAssetIdUseCase.Execute(assetId);
    }
}
