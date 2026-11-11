using Tools.Api.Modules.Translation.Application.Ports;

namespace Tools.Api.Modules.Translation.Application.Views;

// `Source` : la langue demandée, ou celle détectée. `Detected` n'est présent qu'en détection auto.
public sealed record TranslationView(
    IReadOnlyList<string> Texts,
    string? Source,
    string Target,
    DetectedLanguage? Detected,
    int Characters,
    long DurationMs,
    string Provider);
