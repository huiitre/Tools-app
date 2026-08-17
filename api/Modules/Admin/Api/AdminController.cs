using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Admin.Application.Dto;
using Tools.Api.Modules.Admin.Application.Usecases;

namespace Tools.Api.Modules.Admin.Api;

// Tableau de bord d'administration.
//
// Ce module ne détient aucune ressource : il agrège ce que possèdent Users et Access. C'est
// pourquoi il ne s'appelle pas d'après une entité, et pourquoi il ne fait que lire.
//
// Le use case est résolu par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, une action ne doit donc construire que celui dont elle se sert.
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    [HttpGet("stats")]
    public Task<AdminStatsDto> Stats([FromServices] GetAdminStatsUseCase getAdminStatsUseCase)
    {
        return getAdminStatsUseCase.Execute();
    }
}
