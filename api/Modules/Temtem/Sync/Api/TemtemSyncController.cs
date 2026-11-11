using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Temtem.Sync.Application;
using Tools.Api.Modules.Temtem.Sync.Application.Usecases;

namespace Tools.Api.Modules.Temtem.Sync.Api;

// Appelée par l'extracteur Temtem après un scraping, une fois par mise à jour du jeu détectée.
// TECH ou clé d'API temtem:sync : le contrôle vit dans le use case.
[ApiController]
[Route("internal/temtem/sync")]
public sealed class TemtemSyncController : ControllerBase
{
    [HttpPost]
    public Task<TemtemCatalogueSyncReport> Sync(
        [FromServices] SyncTemtemCatalogueUseCase syncTemtemCatalogueUseCase
    )
    {
        return syncTemtemCatalogueUseCase.Execute();
    }
}
