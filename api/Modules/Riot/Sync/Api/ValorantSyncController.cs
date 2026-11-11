using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Sync.Application.Usecases;

namespace Tools.Api.Modules.Riot.Sync.Api;

// Synchronisation du catalogue Valorant depuis les assets locaux. TECH ou clé d'API riot.valorant:sync.
[ApiController]
[Route("internal/riot/valorant/sync")]
public class ValorantSyncController : ControllerBase
{
    [HttpPost]
    public Task<ValorantGlobalSyncReport> Sync(
        [FromServices] SyncValorantUseCase syncValorantUseCase
    )
    {
        return syncValorantUseCase.Execute();
    }
}
