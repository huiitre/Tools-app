using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Users.Application.Usecases;
using Tools.ApiCore.Modules.Users.Application.Dto;

namespace Tools.ApiCore.Modules.Users.Api;

// Profils utilisateur. `me` est un identifiant qui se résout au porteur du jeton : il
// appartient donc à la même famille que le futur `/users/{id}` réservé aux administrateurs.
//
// Ce qui touche aux moyens d'identification — mot de passe, session, providers — relève
// d'AuthController, pas d'ici.
[ApiController]
[Route("users")]
public class UsersController(GetMyProfileUseCase getMyProfileUseCase) : ControllerBase
{
    [HttpGet("me")]
    public Task<UserProfileDto> Me() => getMyProfileUseCase.Execute();
}
