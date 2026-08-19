using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Common.Api.Internal;
using Tools.Api.Modules.GameServers.Application;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Api;

[ApiController]
[Route("internal/gameservers/sync")]
[AllowAnonymous]
[InternalApi]
public class GameServersSyncController : ControllerBase
{
    [HttpPost]
    public Task<GameServersSyncReport> Sync(
        [FromBody] IReadOnlyList<GameServerSyncDto> gameServers,
        [FromServices] GameServersSyncUseCase gameServersSyncUseCase)
    {
        return gameServersSyncUseCase.Execute(gameServers);
    }
}
