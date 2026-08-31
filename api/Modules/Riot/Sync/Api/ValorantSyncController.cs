using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Sync.Application.Usecases;

namespace Tools.Api.Modules.Riot.Sync.Api;

// Synchronisation du catalogue Valorant depuis les assets locaux.
[ApiController]
[Route("internal/riot/valorant/sync")]
[AllowAnonymous]
[InternalApi]
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
