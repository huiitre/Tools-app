using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Translation.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Moteur sans réseau : « traduit » en préfixant la langue cible, détecte toujours le français.
public sealed class FakeTranslationProvider : ITranslationProvider
{
    public bool Unavailable { get; set; }
    public IReadOnlyList<string>? LastTexts { get; private set; }

    public string Name => "fake";

    public Task<ProviderTranslation> Translate(
        IReadOnlyList<string> texts, string? source, string target, CancellationToken cancellationToken)
    {
        if (Unavailable)
            throw AppException.Unavailable("TRANSLATION_UNAVAILABLE", "Le service de traduction est indisponible.");

        LastTexts = texts;
        return Task.FromResult(new ProviderTranslation(
            texts.Select(text => $"[{target}] {text}").ToList(),
            source is null ? new DetectedLanguage("fr", 90) : null));
    }

    public Task<IReadOnlyList<TranslationLanguage>> GetLanguages(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TranslationLanguage>>(
            [new("en", "English", ["fr"]), new("fr", "French", ["en"])]);

    public Task<IReadOnlyList<DetectedLanguage>> Detect(string text, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DetectedLanguage>>([new("fr", 90)]);

    public void Reset()
    {
        Unavailable = false;
        LastTexts = null;
    }
}
