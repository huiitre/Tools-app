using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Huihuistore.Application.Usecases;
using Tools.Api.Modules.Core.Huihuistore.Application.Views;

namespace Tools.Api.Modules.Core.Huihuistore.Api;

// Mise à jour de huihuistore lui-même : publiée par sa CI, lue par l'app.
//
// Les use cases sont résolus par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, une action ne doit donc construire que celui dont elle se sert.
[ApiController]
[Route("huihuistore")]
public sealed class HuihuistoreController : ControllerBase
{
    // Même plafond que l'import des apps du store.
    private const long MaxApkBytes = 200L * 1024 * 1024;

    [HttpGet("update")]
    [AllowAnonymous]
    public Task<HuihuistoreUpdateView> Update(
        [FromServices] GetHuihuistoreUpdateUseCase getHuihuistoreUpdateUseCase,
        CancellationToken cancellationToken)
    {
        return getHuihuistoreUpdateUseCase.Execute(cancellationToken);
    }

    // Appelée par la CI (en-tête X-Api-Key, scope huihuistore:publish) ou par un admin.
    [HttpPost("releases")]
    [RequestSizeLimit(MaxApkBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxApkBytes)]
    public async Task<HuihuistoreUpdateView> Publish(
        [FromServices] PublishHuihuistoreReleaseUseCase publishHuihuistoreReleaseUseCase,
        [FromForm] PublishRequest request,
        CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();

        return await publishHuihuistoreReleaseUseCase.Execute(content, request.ReleaseNotes, cancellationToken);
    }

    public sealed record PublishRequest([Required] IFormFile File, string? ReleaseNotes);
}
