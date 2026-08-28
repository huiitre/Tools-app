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

    // Les paramètres sont libres : chaque jeu déclare les siens, le contrôleur ne les connaît pas.
    [HttpPost("actions/{actionCode}")]
    public async Task<IActionResult> ExecuteAction(
        string slug,
        string actionCode,
        [FromBody] Dictionary<string, string>? parameters,
        [FromServices] GetGameServerDashboardUseCase getGameServerDashboardUseCase,
        CancellationToken cancellationToken)
    {
        await getGameServerDashboardUseCase.ExecuteAction(
            slug,
            actionCode,
            parameters ?? [],
            cancellationToken);

        return NoContent();
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
