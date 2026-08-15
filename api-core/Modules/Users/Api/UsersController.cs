using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.ApiCore.Modules.Users.Application;
using Tools.ApiCore.Modules.Users.Application.Usecases;
using Tools.ApiCore.Modules.Users.Application.Dto;

namespace Tools.ApiCore.Modules.Users.Api;

// Profils utilisateur. `me` est un identifiant qui se résout au porteur du jeton : il
// appartient donc à la même famille que `/users/{id}`, réservé aux administrateurs. Une seule
// ressource, un seul préfixe — l'API Java sépare `/user` de `/users` et doit trancher, à
// chaque nouvel endpoint, de quel côté il tombe.
//
// Ce qui touche aux moyens d'identification — mot de passe, session, providers — relève
// d'AuthController, pas d'ici.
[ApiController]
[Route("users")]
public class UsersController(
    GetMyProfileUseCase getMyProfileUseCase,
    ListUsersUseCase listUsersUseCase,
    SetUserGlobalRoleUseCase setUserGlobalRoleUseCase) : ControllerBase
{
    [HttpGet("me")]
    public Task<UserProfileDto> Me() => getMyProfileUseCase.Execute();

    [HttpGet]
    public Task<IReadOnlyList<UserAdminDto>> List() => listUsersUseCase.Execute();

    [HttpPut("{userId:long}/role")]
    public async Task<IActionResult> SetRole(long userId, SetUserRoleRequest request)
    {
        await setUserGlobalRoleUseCase.Execute(new SetUserGlobalRoleCommand(userId, request.RoleId));
        return NoContent();
    }
}

// DTO entrant : ASP.NET applique cette règle avant d'appeler SetRole.
public sealed record SetUserRoleRequest([Required] long RoleId);

