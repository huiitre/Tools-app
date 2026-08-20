using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Security.Application.Dto;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.Security.Api;

// Catalogue des rôles attribuables.
//
// Il est servi par le module Security parce que c'est là que vit `RoleCode` : la table
// tools_core.role en est la contrepartie persistée. Users et Access le consomment tous les
// deux, l'un pour le rôle global, l'autre pour le rôle contextuel d'un module.
// Le use case est résolu par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, une action ne doit donc construire que celui dont elle se sert.
[ApiController]
[Route("roles")]
public class RolesController : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RoleDto>> List([FromServices] ListRolesUseCase listRolesUseCase)
    {
        return listRolesUseCase.Execute();
    }
}
