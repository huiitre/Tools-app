using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Admin.Application.Dto;
using Tools.ApiCore.Modules.Admin.Application.Usecases;

namespace Tools.ApiCore.Modules.Admin.Api;

// Tableau de bord d'administration.
//
// Ce module ne détient aucune ressource : il agrège ce que possèdent Users et Access. C'est
// pourquoi il ne s'appelle pas d'après une entité, et pourquoi il ne fait que lire.
[ApiController]
[Route("admin")]
public class AdminController(GetAdminStatsUseCase getAdminStatsUseCase) : ControllerBase
{
    [HttpGet("stats")]
    public Task<AdminStatsDto> Stats() => getAdminStatsUseCase.Execute();
}
