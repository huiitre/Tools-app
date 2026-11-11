using System.Text.RegularExpressions;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Translation.Domain;

// Une demande de traduction valide : des textes, une langue cible, une langue source facultative.
// Un seul texte (page Traduction) ou plusieurs fragments d'une page (extension), traduits dans l'ordre.
public sealed partial class TranslationRequest
{
    // Au-delà, une requête bloque la file de LibreTranslate plusieurs secondes : mesuré à ~630
    // caractères/s, soit ~16 s pour 5 000 caractères quand la paire passe par l'anglais.
    public const int MaxCharacters = 5000;

    // Codes de langue ISO 639 (« fr », « en »), avec une variante éventuelle (« zh-Hans »).
    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z]{2,4})?$")]
    private static partial Regex LanguageCodePattern();

    public IReadOnlyList<string> Texts { get; }

    // Nul : détection automatique par le fournisseur.
    public string? Source { get; }
    public string Target { get; }

    // Total de tous les textes, celui que la limite encadre.
    public int Characters { get; }

    private TranslationRequest(IReadOnlyList<string> texts, string? source, string target, int characters)
    {
        Texts = texts;
        Source = source;
        Target = target;
        Characters = characters;
    }

    public static TranslationRequest Create(IReadOnlyList<string>? texts, string? source, string? target)
    {
        if (texts is null || texts.Count == 0 || texts.All(string.IsNullOrWhiteSpace))
            throw AppException.Validation("TRANSLATION_TEXT_REQUIRED", "Le texte à traduire est obligatoire.");

        if (texts.Any(text => text is null))
            throw AppException.Validation("TRANSLATION_TEXT_INVALID", "Un des textes à traduire est vide.");

        var characters = texts.Sum(text => text.Length);
        if (characters > MaxCharacters)
            throw AppException.Validation(
                "TRANSLATION_TEXT_TOO_LONG",
                $"Le texte dépasse la limite de {MaxCharacters} caractères ({characters}).");

        var normalizedTarget = NormalizeLanguage(target)
            ?? throw AppException.Validation("TRANSLATION_TARGET_REQUIRED", "La langue cible est obligatoire.");

        var normalizedSource = NormalizeLanguage(source);
        if (normalizedSource == "auto")
            normalizedSource = null;

        if (normalizedSource == normalizedTarget)
            throw AppException.Validation(
                "TRANSLATION_SAME_LANGUAGE", "La langue source et la langue cible sont identiques.");

        return new TranslationRequest(texts, normalizedSource, normalizedTarget, characters);
    }

    // Nul pour une valeur absente ; refuse ce qui ne ressemble pas à un code de langue. Que la
    // langue soit réellement proposée, c'est au fournisseur d'en juger.
    private static string? NormalizeLanguage(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = code.Trim();
        if (normalized == "auto")
            return normalized;

        if (!LanguageCodePattern().IsMatch(normalized))
            throw AppException.Validation("TRANSLATION_LANGUAGE_INVALID", $"« {normalized} » n'est pas un code de langue.");

        return normalized;
    }
}
