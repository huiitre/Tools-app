using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Translation.Application.Commands;
using Tools.Api.Modules.Translation.Application.Ports;
using Tools.Api.Modules.Translation.Application.Usecases;
using Tools.Api.Modules.Translation.Application.Views;

namespace Tools.Api.Modules.Translation.Api;

// Membres du module (READ_ONLY) ou clé d'API translation:translate (extension navigateur).
[ApiController]
[Route("translation")]
public sealed class TranslationController : ControllerBase
{
    [HttpPost("translate")]
    public Task<TranslationView> Translate(
        TranslateRequest request,
        [FromServices] TranslateUseCase translateUseCase,
        CancellationToken cancellationToken)
    {
        return translateUseCase.Execute(
            new TranslateCommand(request.Texts, request.Source, request.Target), cancellationToken);
    }

    [HttpGet("languages")]
    public Task<IReadOnlyList<TranslationLanguage>> Languages(
        [FromServices] ListTranslationLanguagesUseCase listTranslationLanguagesUseCase,
        CancellationToken cancellationToken)
    {
        return listTranslationLanguagesUseCase.Execute(cancellationToken);
    }

    [HttpPost("detect")]
    public Task<IReadOnlyList<DetectedLanguage>> Detect(
        DetectRequest request,
        [FromServices] DetectLanguageUseCase detectLanguageUseCase,
        CancellationToken cancellationToken)
    {
        return detectLanguageUseCase.Execute(request.Text, cancellationToken);
    }

    // Un seul texte (page Traduction) ou les fragments d'une page (extension), dans l'ordre.
    public sealed record TranslateRequest(IReadOnlyList<string>? Texts, string? Source, string? Target);

    public sealed record DetectRequest(string? Text);
}
