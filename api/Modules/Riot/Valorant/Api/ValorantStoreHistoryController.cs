using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.User.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.User.Views;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Historique des rotations de boutique, par compte Valorant lié.
[ApiController]
[Route("riot/valorant/store-history")]
public class ValorantStoreHistoryController : ControllerBase
{
    [HttpGet]
    public Task<List<ValorantStoreHistoryView>> GetMyStoreHistory(
        [FromServices] GetMyValorantStoreHistoryUseCase getMyValorantStoreHistoryUseCase,
        [FromQuery] long accountId
    )
    {
        return getMyValorantStoreHistoryUseCase.Execute(accountId);
    }

    [HttpPost]
    public async Task<IActionResult> AddToStoreHistory(
        [FromServices] AddSkinToStoreHistoryUseCase addSkinToStoreHistoryUseCase,
        [FromBody] AddSkinToStoreHistoryCommand command
    )
    {
        await addSkinToStoreHistoryUseCase.Execute(command);

        return StatusCode(StatusCodes.Status201Created);
    }
}
