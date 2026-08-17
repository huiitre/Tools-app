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
//
// Les use cases sont résolus par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, et ce contrôleur mélange deux niveaux d'exigence — `me` se
// contente de READ_ONLY quand les deux autres routes réclament ADMIN. Injectés au constructeur,
// ils seraient tous construits à chaque requête et `me` deviendrait réservé aux administrateurs.
[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public Task<UserProfileDto> Me([FromServices] GetMyProfileUseCase getMyProfileUseCase)
    {
        return getMyProfileUseCase.Execute();
    }

    [HttpGet]
    public Task<IReadOnlyList<UserAdminDto>> List([FromServices] ListUsersUseCase listUsersUseCase)
    {
        return listUsersUseCase.Execute();
    }

    [HttpPut("{userId:long}/role")]
    public async Task<IActionResult> SetRole(
        long userId,
        SetUserRoleRequest request,
        [FromServices] SetUserGlobalRoleUseCase setUserGlobalRoleUseCase)
    {
        await setUserGlobalRoleUseCase.Execute(new SetUserGlobalRoleCommand(userId, request.RoleId));
        return NoContent();
    }
}

// DTO entrant : ASP.NET applique cette règle avant d'appeler SetRole.
public sealed record SetUserRoleRequest([Required] long RoleId);
