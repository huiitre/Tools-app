using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;
using Tools.Api.Modules.Riot.Valorant.Application.User.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Skins suivis, par compte Valorant lié.
[ApiController]
[Route("riot/valorant/watchlist")]
public class ValorantWatchlistController : ControllerBase
{
    [HttpGet]
    public Task<List<ValorantSkinView>> GetMyWatchlist(
        [FromServices] GetMyValorantWatchlistUseCase getMyValorantWatchlistUseCase,
        [FromQuery] long accountId
    )
    {
        return getMyValorantWatchlistUseCase.Execute(accountId);
    }

    [HttpPost]
    public async Task<IActionResult> AddToWatchlist(
        [FromServices] AddSkinToWatchlistUseCase addSkinToWatchlistUseCase,
        [FromBody] AddToWatchlistCommand command
    )
    {
        var skin = await addSkinToWatchlistUseCase.Execute(command);

        return StatusCode(StatusCodes.Status201Created, skin);
    }

    [HttpDelete("{skinId:long}")]
    public async Task<IActionResult> RemoveFromWatchlist(
        [FromServices] RemoveSkinFromWatchlistUseCase removeSkinFromWatchlistUseCase,
        [FromRoute] long skinId,
        [FromQuery] long accountId
    )
    {
        await removeSkinFromWatchlistUseCase.Execute(skinId, accountId);

        return NoContent();
    }

    // Déclenche à la main la passe que le planificateur fait seul. Le contrôle d'accès est dans
    // le use case, pas dans le notifieur qu'il appelle — celui-ci tourne aussi sans utilisateur.
    [HttpPost("admin/sync")]
    public async Task<IActionResult> TriggerSync(
        [FromServices] TriggerValorantWatchlistSyncUseCase triggerValorantWatchlistSyncUseCase
    )
    {
        await triggerValorantWatchlistSyncUseCase.Execute();

        return NoContent();
    }
}
