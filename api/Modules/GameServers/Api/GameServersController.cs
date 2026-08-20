using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.GameServers.Application;
using Tools.Api.Modules.GameServers.Application.Dto;

namespace Tools.Api.Modules.GameServers.Api;

[ApiController]
[Route("gameservers")]
public class GameServersController : ControllerBase
{
    // Snapshot DB uniquement. Le scheduler est l'unique responsable des appels réseau de statut.
    [HttpGet]
    public Task<IReadOnlyList<GameServerDashboardView>> Get(
        [FromServices] GetGameServersUseCase getGameServersUseCase)
    {
        return getGameServersUseCase.Execute();
    }
}
