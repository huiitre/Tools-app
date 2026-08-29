using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Boutique du jour, lue chez Riot. Deux modes : un compte lié (accountId, l'API retrouve ses
// jetons) ou un jeton fourni à la volée par l'appelant (X-Riot-Token + region).
[ApiController]
[Route("riot/valorant")]
public class ValorantStoreController : ControllerBase
{
    [HttpGet("store")]
    public Task<ValorantStoreView> GetStore(
        [FromServices] GetValorantStoreUseCase getValorantStoreUseCase,
        [FromQuery] long? accountId,
        [FromHeader(Name = "X-Riot-Token")] string? accessToken,
        [FromQuery] string? region
    )
    {
        return getValorantStoreUseCase.Execute(accountId, accessToken, region);
    }
}
