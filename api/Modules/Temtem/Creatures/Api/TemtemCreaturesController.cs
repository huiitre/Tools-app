using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Temtem.Creatures.Application.Usecases;
using Tools.Api.Modules.Temtem.Creatures.Application.Views;

namespace Tools.Api.Modules.Temtem.Creatures.Api;

// Catalogue des Temtem.
//
// Le segment « creatures » évite l'ambiguïté qu'aurait eue /temtem/{slug} avec /temtem/types :
// un Temtem dont le slug serait « types » aurait masqué le référentiel.
[ApiController]
[Route("temtem/creatures")]
public class TemtemCreaturesController : ControllerBase
{
    [HttpGet]
    public Task<List<TemtemSummaryView>> List(
        [FromServices] ListTemtemCreaturesUseCase listTemtemCreaturesUseCase
    )
    {
        return listTemtemCreaturesUseCase.Execute();
    }

    [HttpGet("{slug}")]
    public Task<TemtemDetailView> Get(
        [FromServices] GetTemtemBySlugUseCase getTemtemBySlugUseCase,
        [FromRoute] string slug
    )
    {
        return getTemtemBySlugUseCase.Execute(slug);
    }
}
