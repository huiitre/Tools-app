using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Health.Application;

namespace Tools.ApiCore.Modules.Health.Api;

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
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        logger.LogDebug("Vérification de readiness demandée.");

        var isReady = await checkReadinessUseCase.Execute(cancellationToken);
        var response = new { status = isReady ? "healthy" : "unhealthy" };

        return isReady ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
