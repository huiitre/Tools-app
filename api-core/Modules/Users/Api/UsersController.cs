using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly ListUsersUseCase listUsersUseCase;
    private readonly SetUserPasswordUseCase setUserPasswordUseCase;
    private readonly ILogger<UsersController> logger;

    public UsersController(
        ListUsersUseCase listUsersUseCase,
        SetUserPasswordUseCase setUserPasswordUseCase,
        ILogger<UsersController> logger)
    {
        this.listUsersUseCase = listUsersUseCase;
        this.setUserPasswordUseCase = setUserPasswordUseCase;
        this.logger = logger;
    }

    [HttpPatch("password")]
    public async Task<IActionResult> SetPassword(SetPasswordRequest request, CancellationToken cancellationToken)
    {
        await setUserPasswordUseCase.Execute(new SetUserPasswordCommand(request.Password), cancellationToken);
        return NoContent();
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

public sealed record SetPasswordRequest([Required] string Password);
