using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;
using Tools.Api.Modules.Riot.Valorant.Application.User.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Skins possédés, par compte Valorant lié.
[ApiController]
[Route("riot/valorant/my-skins")]
public class ValorantUserSkinController : ControllerBase
{
    [HttpGet]
    public Task<List<ValorantSkinView>> GetMyUserSkins(
        [FromServices] GetMyValorantUserSkinsUseCase getMyValorantUserSkinsUseCase,
        [FromQuery] long accountId
    )
    {
        return getMyValorantUserSkinsUseCase.Execute(accountId);
    }

    [HttpPost]
    public async Task<IActionResult> AddMySkin(
        [FromServices] AddMyValorantSkinUseCase addMyValorantSkinUseCase,
        [FromBody] AddUserSkinCommand command
    )
    {
        var skin = await addMyValorantSkinUseCase.Execute(command);

        return StatusCode(StatusCodes.Status201Created, skin);
    }

    [HttpDelete("{skinId:long}")]
    public async Task<IActionResult> RemoveMySkin(
        [FromServices] RemoveMyValorantSkinUseCase removeMyValorantSkinUseCase,
        [FromRoute] long skinId,
        [FromQuery] long accountId
    )
    {
        await removeMyValorantSkinUseCase.Execute(skinId, accountId);

        return NoContent();
    }
}
