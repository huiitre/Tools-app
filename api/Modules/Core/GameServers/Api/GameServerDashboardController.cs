using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Api;

// Interrogation en direct d'un serveur, par opposition à GameServersController qui ne sert que le
// snapshot du poll/live-state. Details est chargé une fois à l'ouverture ; le live (joueurs,
// position, journal…) ne passe plus par ici depuis le push WebSocket (Core.GameServersLiveUpdated,
// voir PollGameServersUseCase) — GET .../live a été retiré, remplacé par ce push et le cold-start
// GET /gameservers/live-state.
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

    // Réservée aux jeux qui l'annoncent (details.supportsRawCommand) et au rôle ADMIN, revérifiés
    // tous deux côté use case : le contrôleur ne fait que transmettre.
    [HttpPost("raw-command")]
    public async Task<GameServerRawCommandResult> ExecuteRawCommand(
        string slug,
        [FromBody] GameServerRawCommandRequest request,
        [FromServices] GetGameServerDashboardUseCase getGameServerDashboardUseCase,
        CancellationToken cancellationToken)
    {
        var answer = await getGameServerDashboardUseCase.ExecuteRawCommand(slug, request.Command, cancellationToken);
        return new GameServerRawCommandResult(answer);
    }

    // Même garde que l'exécution : révèle ce que d'autres admins ont tapé, pas seulement soi-même.
    [HttpGet("raw-command/history")]
    public Task<IReadOnlyList<GameServerRawCommandHistoryEntry>> GetRawCommandHistory(
        string slug,
        [FromServices] GetGameServerDashboardUseCase getGameServerDashboardUseCase,
        CancellationToken cancellationToken)
    {
        return getGameServerDashboardUseCase.ExecuteRawCommandHistory(slug, cancellationToken);
    }
}
