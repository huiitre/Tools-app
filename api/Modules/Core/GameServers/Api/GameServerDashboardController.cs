using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Api;

// Interrogation en direct d'un serveur, par opposition à GameServersController qui ne sert que le
// snapshot du poll. Details est chargé à l'ouverture, live est rafraîchi.
[ApiController]
[Route("gameservers/{slug}")]
public class GameServerDashboardController : ControllerBase
{
    [HttpGet("details")]
    public Task<GameServerDetailsView> GetDetails(
        string slug,
        [FromServices] GetGameServerDashboardUseCase getGameServerDashboardUseCase,
        CancellationToken cancellationToken)
    {
        return getGameServerDashboardUseCase.ExecuteDetails(slug, cancellationToken);
    }

    [HttpGet("live")]
    public Task<GameServerLiveView> GetLive(
        string slug,
        [FromServices] GetGameServerDashboardUseCase getGameServerDashboardUseCase,
        CancellationToken cancellationToken)
    {
        return getGameServerDashboardUseCase.ExecuteLive(slug, cancellationToken);
    }
}
