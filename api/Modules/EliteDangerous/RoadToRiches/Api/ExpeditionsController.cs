using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Commands;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Views;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Api;

// Expéditions Road to Riches d'Elite Dangerous.
//
// Les use cases sont résolus par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, une action ne doit donc construire que celui dont elle se sert.
[ApiController]
[Route("elite-dangerous/expeditions")]
public class ExpeditionsController : ControllerBase
{
    [HttpGet]
    public Task<List<ExpeditionSummaryView>> List(
        [FromServices] ListExpeditionsUseCase listExpeditionsUseCase
    )
    {
        return listExpeditionsUseCase.Execute();
    }

    [HttpGet("{expeditionId:guid}")]
    public Task<ExpeditionDetailView> Get(
        [FromServices] GetExpeditionUseCase getExpeditionUseCase,
        [FromRoute] Guid expeditionId
    )
    {
        return getExpeditionUseCase.Execute(expeditionId);
    }

    // Le fichier arrive en multipart : sa lecture est la seule entrée/sortie de l'import, l'analyse
    // qui suit ne travaille que sur des octets déjà en mémoire.
    [HttpPost]
    public async Task<IActionResult> Import(
        [FromServices] ImportExpeditionUseCase importExpeditionUseCase,
        [FromForm] ImportExpeditionRequest request
    )
    {
        using var content = new MemoryStream();
        await request.File.CopyToAsync(content);

        var expeditionId = await importExpeditionUseCase.Execute(new ImportExpeditionCommand(
            content.ToArray(),
            request.File.FileName,
            request.Source,
            request.Name));

        return CreatedAtAction(nameof(Get), new { expeditionId }, new { id = expeditionId });
    }

    [HttpPatch("{expeditionId:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(
        [FromServices] UpdateProgressUseCase updateProgressUseCase,
        [FromRoute] Guid expeditionId,
        UpdateProgressRequest request
    )
    {
        // Le tableau absent vaut « aucun corps fait » : la colonne est NOT NULL.
        await updateProgressUseCase.Execute(
            expeditionId,
            new UpdateProgressCommand(request.CurrentSystemIndex, request.CurrentBodiesDone ?? []));

        return NoContent();
    }

    [HttpPatch("{expeditionId:guid}/name")]
    public async Task<IActionResult> Rename(
        [FromServices] RenameExpeditionUseCase renameExpeditionUseCase,
        [FromRoute] Guid expeditionId,
        RenameExpeditionRequest request
    )
    {
        await renameExpeditionUseCase.Execute(expeditionId, new RenameExpeditionCommand(request.Name));

        return NoContent();
    }

    [HttpGet("{expeditionId:guid}/export")]
    public async Task<IActionResult> Export(
        [FromServices] ExportExpeditionUseCase exportExpeditionUseCase,
        [FromRoute] Guid expeditionId
    )
    {
        var routeData = await exportExpeditionUseCase.Execute(expeditionId);

        return File(
            Encoding.UTF8.GetBytes(routeData),
            "application/json",
            $"expedition-{expeditionId}.json");
    }

    [HttpDelete("{expeditionId:guid}")]
    public async Task<IActionResult> Delete(
        [FromServices] DeleteExpeditionUseCase deleteExpeditionUseCase,
        [FromRoute] Guid expeditionId
    )
    {
        await deleteExpeditionUseCase.Execute(expeditionId);

        return NoContent();
    }
}

// DTO entrants : ASP.NET applique ces règles avant d'appeler l'action.
public sealed record ImportExpeditionRequest(
    [Required] IFormFile File,
    [Required] string Source,
    string? Name);

public sealed record UpdateProgressRequest(
    int CurrentSystemIndex,
    List<long>? CurrentBodiesDone);

public sealed record RenameExpeditionRequest([Required] string Name);
