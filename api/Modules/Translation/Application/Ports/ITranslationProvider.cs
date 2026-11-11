namespace Tools.Api.Modules.Translation.Application.Ports;

// Un moteur de traduction. LibreTranslate est le premier ; un LLM pourra s'y brancher sans
// toucher aux use cases. Les erreurs sortent en AppException (langue refusée, moteur injoignable).
public interface ITranslationProvider
{
    // Nom stable, écrit dans le journal et renvoyé au client : « libretranslate ».
    string Name { get; }

    // Un texte traduit par texte reçu, dans le même ordre. `source` nul : détection automatique.
    Task<ProviderTranslation> Translate(
        IReadOnlyList<string> texts, string? source, string target, CancellationToken cancellationToken);

    Task<IReadOnlyList<TranslationLanguage>> GetLanguages(CancellationToken cancellationToken);

    // Langues candidates, la plus probable en premier.
    Task<IReadOnlyList<DetectedLanguage>> Detect(string text, CancellationToken cancellationToken);
}

// `Detected` n'est renseigné que si la source était à détecter.
public sealed record ProviderTranslation(IReadOnlyList<string> Texts, DetectedLanguage? Detected);

public sealed record DetectedLanguage(string Language, double Confidence);

// `Targets` : les langues vers lesquelles celle-ci peut être traduite.
public sealed record TranslationLanguage(string Code, string Name, IReadOnlyList<string> Targets);
