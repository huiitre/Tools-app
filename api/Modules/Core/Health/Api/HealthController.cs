using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Health.Application;

namespace Tools.Api.Modules.Core.Health.Api;

[ApiController]
[Route("health")]
// Sondes appelées par le healthcheck du conteneur et par Watchtower, sans jeton.
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly CheckReadinessUseCase checkReadinessUseCase;
    private readonly ILogger<HealthController> logger;

    public HealthController(
        CheckReadinessUseCase checkReadinessUseCase,
        ILogger<HealthController> logger)
    {
        this.checkReadinessUseCase = checkReadinessUseCase;
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        logger.LogDebug("Vérification de liveness demandée.");

        return Ok(new { status = "healthy" });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        logger.LogDebug("Vérification de readiness demandée.");

        var isReady = await checkReadinessUseCase.Execute();
        var response = new { status = isReady ? "healthy" : "unhealthy" };

        return isReady ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
