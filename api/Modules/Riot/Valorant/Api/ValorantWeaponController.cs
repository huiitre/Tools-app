using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Catalogue des armes.
[ApiController]
[Route("riot/valorant")]
public class ValorantWeaponController : ControllerBase
{
    [HttpGet("weapons")]
    public Task<List<ValorantWeaponView>> ListWeapons(
        [FromServices] ListValorantWeaponsUseCase listValorantWeaponsUseCase
    )
    {
        return listValorantWeaponsUseCase.Execute();
    }

    [HttpGet("weapons/{id:long}")]
    public Task<ValorantWeaponView> GetWeapon(
        [FromServices] GetValorantWeaponUseCase getValorantWeaponUseCase,
        [FromRoute] long id
    )
    {
        return getValorantWeaponUseCase.Execute(id);
    }

    // accountId facultatif : sans lui, « possédé » et « suivi » sont faux.
    [HttpGet("weapons/{id:long}/skins")]
    public Task<List<ValorantSkinView>> GetWeaponSkins(
        [FromServices] GetValorantWeaponSkinsUseCase getValorantWeaponSkinsUseCase,
        [FromRoute] long id,
        [FromQuery] long? accountId
    )
    {
        return getValorantWeaponSkinsUseCase.Execute(id, accountId);
    }
}
