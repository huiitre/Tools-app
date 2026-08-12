using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> logger;

    public HealthController(
        ILogger<HealthController> logger
    )
    {
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [Route("live")]
    public IActionResult Live()
    {
        return
    }
}