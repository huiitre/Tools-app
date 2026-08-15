using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Security.Application.Dto;
using Tools.ApiCore.Modules.Security.Application.Usecases;

namespace Tools.ApiCore.Modules.Security.Api;

// Catalogue des rôles attribuables.
//
// Il est servi par le module Security parce que c'est là que vit `RoleCode` : la table
// tools_core.role en est la contrepartie persistée. Users et Access le consomment tous les
// deux, l'un pour le rôle global, l'autre pour le rôle contextuel d'un module.
[ApiController]
[Route("roles")]
public class RolesController(ListRolesUseCase listRolesUseCase) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RoleDto>> List() => listRolesUseCase.Execute();
}
