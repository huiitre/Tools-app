using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Catalogue des skins. accountId est facultatif partout : il ne sert qu'à renseigner « possédé »
// et « suivi » sur chaque skin.
[ApiController]
[Route("riot/valorant")]
public class ValorantSkinController : ControllerBase
{
    [HttpGet("skins")]
    public Task<List<ValorantSkinView>> ListSkins(
        [FromServices] ListValorantSkinsUseCase listValorantSkinsUseCase,
        [FromQuery] long? accountId
    )
    {
        return listValorantSkinsUseCase.Execute(accountId);
    }

    [HttpGet("skins/{id:long}")]
    public Task<ValorantSkinView> GetSkin(
        [FromServices] GetValorantSkinUseCase getValorantSkinUseCase,
        [FromRoute] long id,
        [FromQuery] long? accountId
    )
    {
        return getValorantSkinUseCase.Execute(id, accountId);
    }

    [HttpGet("skins/by-asset/{assetId:guid}")]
    public Task<ValorantSkinView> GetSkinByAssetId(
        [FromServices] GetValorantSkinByAssetIdUseCase getValorantSkinByAssetIdUseCase,
        [FromRoute] Guid assetId,
        [FromQuery] long? accountId
    )
    {
        return getValorantSkinByAssetIdUseCase.Execute(assetId, accountId);
    }

    // Le storefront Riot ne renvoie que des UUID de *levels*, jamais l'UUID du skin racine :
    // c'est cette route qui fait le pont.
    [HttpGet("skins/by-level/{levelAssetId:guid}")]
    public Task<ValorantSkinView> GetSkinByLevel(
        [FromServices] GetValorantSkinByLevelUseCase getValorantSkinByLevelUseCase,
        [FromRoute] Guid levelAssetId,
        [FromQuery] long? accountId
    )
    {
        return getValorantSkinByLevelUseCase.Execute(levelAssetId, accountId);
    }

    [HttpGet("skins/by-theme/{themeUuid:guid}")]
    public Task<List<ValorantSkinView>> ListSkinsByTheme(
        [FromServices] ListValorantSkinsByThemeUseCase listValorantSkinsByThemeUseCase,
        [FromRoute] Guid themeUuid,
        [FromQuery] long? accountId
    )
    {
        return listValorantSkinsByThemeUseCase.Execute(themeUuid, accountId);
    }
}
