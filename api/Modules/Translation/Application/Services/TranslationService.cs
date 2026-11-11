using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Translation.Application.Ports;
using Tools.Api.Modules.Translation.Domain;

namespace Tools.Api.Modules.Translation.Application.Services;

// Traduire, et rien d'autre : ce qui se passe avant (préparation) et après (mise en forme,
// journal) appartient à chaque use case. Sert aussi aux autres modules, sans passer par HTTP.
public sealed class TranslationService(ITranslationProvider provider)
{
    public string ProviderName => provider.Name;

    public async Task<ProviderTranslation> Translate(TranslationRequest request, CancellationToken cancellationToken)
    {
        var result = await provider.Translate(request.Texts, request.Source, request.Target, cancellationToken);

        // Un texte par texte reçu : sinon la traduction d'une page se décalerait d'un fragment.
        if (result.Texts.Count != request.Texts.Count)
            throw AppException.Unavailable(
                "TRANSLATION_PROVIDER_INVALID", "Le service de traduction a rendu une réponse incomplète.");

        return result;
    }

    public Task<IReadOnlyList<TranslationLanguage>> GetLanguages(CancellationToken cancellationToken) =>
        provider.GetLanguages(cancellationToken);

    public Task<IReadOnlyList<DetectedLanguage>> Detect(string? text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw AppException.Validation("TRANSLATION_TEXT_REQUIRED", "Le texte à analyser est obligatoire.");

        if (text.Length > TranslationRequest.MaxCharacters)
            throw AppException.Validation(
                "TRANSLATION_TEXT_TOO_LONG",
                $"Le texte dépasse la limite de {TranslationRequest.MaxCharacters} caractères ({text.Length}).");

        return provider.Detect(text, cancellationToken);
    }
}
