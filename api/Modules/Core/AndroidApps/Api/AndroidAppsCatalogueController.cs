using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.AndroidApps.Application.Usecases;
using Tools.Api.Modules.Core.AndroidApps.Application.Views;

namespace Tools.Api.Modules.Core.AndroidApps.Api;

// Lu par les TV (huihuistore), qui n'ont pas de compte : voir GetAndroidAppCatalogueUseCase.
[ApiController]
[AllowAnonymous]
[Route("android-apps")]
public sealed class AndroidAppsCatalogueController : ControllerBase
{
    [HttpGet("catalogue")]
    public Task<AndroidAppCatalogueView> Catalogue(
        [FromServices] GetAndroidAppCatalogueUseCase getAndroidAppCatalogueUseCase)
    {
        return getAndroidAppCatalogueUseCase.Execute();
    }
}
