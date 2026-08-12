using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly ListUsersUseCase listUsersUseCase;
    private readonly ILogger<UsersController> logger;

    public UsersController(
        ListUsersUseCase listUsersUseCase,
        ILogger<UsersController> logger)
    {
        this.listUsersUseCase = listUsersUseCase;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUsers()
    {
        logger.LogInformation("La création de trois utilisateurs a été demandée.");

        await listUsersUseCase.Execute();

        logger.LogInformation("La création de trois utilisateurs est terminée.");

        return NoContent();
    }
}
