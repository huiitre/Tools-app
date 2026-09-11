using Microsoft.AspNetCore.Mvc;
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

    // Snapshot DB lui aussi : mods, icônes et modpack sont écrits par le sync.
    [HttpGet("{slug}/mods")]
    public Task<GameServerModsView> GetMods(
        string slug,
        [FromServices] GetGameServerModsUseCase getGameServerModsUseCase)
    {
        return getGameServerModsUseCase.Execute(slug);
    }
}
