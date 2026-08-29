using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Version du client Riot, lue dans les assets locaux.
[ApiController]
[Route("riot/valorant")]
public class ValorantVersionController : ControllerBase
{
    [HttpGet("version")]
    public Task<IReadOnlyDictionary<string, object>> GetVersion(
        [FromServices] GetValorantVersionUseCase getValorantVersionUseCase
    )
    {
        return getValorantVersionUseCase.Execute();
    }
}
