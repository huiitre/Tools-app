using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Temtem.Types.Application.Usecases;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Types.Api;

// Référentiel des types élémentaires : filtres du catalogue, icônes des techniques et matrice
// d'efficacité brute du simulateur.
[ApiController]
[Route("temtem/types")]
public class TemtemTypesController : ControllerBase
{
    [HttpGet]
    public Task<List<TemtemTypeView>> List(
        [FromServices] ListTemtemTypesUseCase listTemtemTypesUseCase
    )
    {
        return listTemtemTypesUseCase.Execute();
    }

    [HttpGet("effectiveness")]
    public Task<List<TemtemTypeEffectivenessView>> ListEffectiveness(
        [FromServices] ListTemtemTypeEffectivenessUseCase listTemtemTypeEffectivenessUseCase
    )
    {
        return listTemtemTypeEffectivenessUseCase.Execute();
    }
}
