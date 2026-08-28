using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Tools.Api.Modules.Core.GameServers.Application.Usecases;

namespace Tools.Api.Modules.Core.GameServers.Api;

[ApiController]
[Route("internal/gameservers/sync")]
[AllowAnonymous]
[InternalApi]
public class GameServersSyncController : ControllerBase
{
    [HttpPost]
    public Task<GameServersSyncReport> Sync(
        [FromServices] GameServersSyncUseCase gameServersSyncUseCase)
    {
        return gameServersSyncUseCase.Execute();
    }
}
