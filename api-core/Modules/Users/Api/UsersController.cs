using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.ApiCore.Modules.Users.Application.Usecases;
using Tools.ApiCore.Modules.Auth.Application.Usecases.Password;
using Tools.ApiCore.Modules.Users.Application.Dto;

namespace Tools.ApiCore.Modules.Users.Api;

[ApiController]
[Route("users")]
public class UsersController(
    SetUserPasswordUseCase setUserPasswordUseCase,
    GetMyProfileUseCase getMyProfileUseCase) : ControllerBase
{
    [HttpGet("me")]
    public Task<UserProfileDto> Me() => getMyProfileUseCase.Execute();

    [HttpPatch("password")]
    public async Task<IActionResult> SetPassword(SetPasswordRequest request)
    {
        await setUserPasswordUseCase.Execute(new SetUserPasswordCommand(request.Password));
        return NoContent();
    }
}

public sealed record SetPasswordRequest([Required] string Password);
