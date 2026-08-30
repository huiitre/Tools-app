using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Temtem.Types.Application.Usecases;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Types.Api;

// Référentiel des types élémentaires : filtres du catalogue et icônes des techniques.
//
// La matrice d'efficacité n'est pas exposée : le calcul des forces et faiblesses vit dans le
// domaine, côté API, et le front n'en reçoit que le résultat.
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
}
