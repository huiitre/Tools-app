using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Common.Api.Internal;
using Tools.Api.Modules.Temtem.Sync.Application;
using Tools.Api.Modules.Temtem.Sync.Application.Usecases;

namespace Tools.Api.Modules.Temtem.Sync.Api;

// Appelée par l'extracteur Temtem après un scraping, une fois par mise à jour du jeu détectée.
// Authentifiée par secret partagé, jamais par jeton utilisateur : sans l'en-tête, la route rend
// 404 et ne confirme donc pas son existence.
[ApiController]
[Route("internal/temtem/sync")]
[AllowAnonymous]
[InternalApi]
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
