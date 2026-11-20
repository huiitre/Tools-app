using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.AndroidApps.Application.Commands;
using Tools.Api.Modules.Core.AndroidApps.Application.Usecases;
using Tools.Api.Modules.Core.AndroidApps.Application.Views;

namespace Tools.Api.Modules.Core.AndroidApps.Api;

// Administration des apps distribuées par huihuistore (onglet de la page admin).
//
// Les use cases sont résolus par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, une action ne doit donc construire que celui dont elle se sert.
[ApiController]
[Route("admin/android-apps")]
public sealed class AndroidAppsAdminController : ControllerBase
{
    // Un APK fait couramment 30 à 100 Mo : au-delà des limites par défaut de Kestrel (~28 Mo) et du
    // multipart (128 Mo). Relevées sur cette seule route.
    private const long MaxApkBytes = 200L * 1024 * 1024;

    [HttpGet]
    public Task<List<AndroidAppView>> List(
        [FromServices] ListAndroidAppsUseCase listAndroidAppsUseCase)
    {
        return listAndroidAppsUseCase.Execute();
    }

    // Premier temps de l'import : rien n'est créé, la réponse alimente la popup.
    [HttpPost("imports")]
    [RequestSizeLimit(MaxApkBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxApkBytes)]
    public async Task<AndroidAppImportAnalysisView> AnalyzeImport(
        [FromServices] AnalyzeAndroidAppImportUseCase analyzeAndroidAppImportUseCase,
        [FromForm] AnalyzeImportRequest request,
        CancellationToken cancellationToken)
    {
        await using var content = request.File.OpenReadStream();

        return await analyzeAndroidAppImportUseCase.Execute(content, cancellationToken);
    }

    [HttpPost("imports/{importId:guid}/confirm")]
    public Task<AndroidAppView> ConfirmImport(
        [FromServices] ConfirmAndroidAppImportUseCase confirmAndroidAppImportUseCase,
        [FromRoute] Guid importId,
        ConfirmImportRequest request,
        CancellationToken cancellationToken)
    {
        return confirmAndroidAppImportUseCase.Execute(
            new ConfirmAndroidAppImportCommand(importId, request.Label, request.Recommend, request.ReleaseNotes),
            cancellationToken);
    }

    [HttpDelete("imports/{importId:guid}")]
    public IActionResult CancelImport(
        [FromServices] CancelAndroidAppImportUseCase cancelAndroidAppImportUseCase,
        [FromRoute] Guid importId)
    {
        cancelAndroidAppImportUseCase.Execute(importId);

        return NoContent();
    }

    [HttpPatch("{appId:long}")]
    public Task<AndroidAppView> Update(
        [FromServices] UpdateAndroidAppUseCase updateAndroidAppUseCase,
        [FromRoute] long appId,
        UpdateAndroidAppRequest request)
    {
        return updateAndroidAppUseCase.Execute(appId, new UpdateAndroidAppCommand(
            request.Label, request.DisplayOrder, request.IsActive, request.RecommendedReleaseId));
    }

    [HttpDelete("{appId:long}/releases/{releaseId:long}")]
    public async Task<IActionResult> DeleteRelease(
        [FromServices] DeleteAndroidAppReleaseUseCase deleteAndroidAppReleaseUseCase,
        [FromRoute] long appId,
        [FromRoute] long releaseId)
    {
        await deleteAndroidAppReleaseUseCase.Execute(appId, releaseId);

        return NoContent();
    }

    // Supprime aussi tous les fichiers APK de l'app : le front l'a fait confirmer.
    [HttpDelete("{appId:long}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteAndroidAppUseCase deleteAndroidAppUseCase,
        [FromRoute] long appId)
    {
        await deleteAndroidAppUseCase.Execute(appId);

        return NoContent();
    }

    public sealed record AnalyzeImportRequest([Required] IFormFile File);

    // Recommend vaut true si absent : la case est cochée par défaut.
    public sealed record ConfirmImportRequest(string? Label, string? ReleaseNotes, bool Recommend = true);

    public sealed record UpdateAndroidAppRequest(
        string? Label,
        int? DisplayOrder,
        bool? IsActive,
        long? RecommendedReleaseId);
}
