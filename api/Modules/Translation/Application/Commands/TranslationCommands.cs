namespace Tools.Api.Modules.Translation.Application.Commands;

// `Source` nul ou « auto » : détection automatique.
public sealed record TranslateCommand(IReadOnlyList<string>? Texts, string? Source, string? Target);
