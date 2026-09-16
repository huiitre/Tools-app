using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Api;

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

    // État en mémoire alimenté par le poll (10s), diffusé par ailleurs via l'event WebSocket
    // Core.GameServersLiveUpdated (voir PollGameServersUseCase) : cette route ne sert qu'à ne pas
    // attendre le prochain tick à la connexion (page qui charge, WebSocket qui vient de s'établir).
    [HttpGet("live-state")]
    public IReadOnlyList<GameServerLiveSnapshot> GetLiveState(
        [FromServices] GetGameServersLiveStateUseCase getGameServersLiveStateUseCase)
    {
        return getGameServersLiveStateUseCase.Execute();
    }

    // Snapshot DB lui aussi : mods, icônes et modpack sont écrits par le sync.
    [HttpGet("{slug}/mods")]
    public Task<GameServerModsView> GetMods(
        string slug,
        [FromServices] GetGameServerModsUseCase getGameServerModsUseCase)
    {
        return getGameServerModsUseCase.Execute(slug);
    }
}
