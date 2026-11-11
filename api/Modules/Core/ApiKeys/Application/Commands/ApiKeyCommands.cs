namespace Tools.Api.Modules.Core.ApiKeys.Application.Commands;

// Le créateur n'est jamais une commande : c'est l'appelant validé.
public sealed record CreateApiKeyCommand(
    string Name,
    string Description,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<string> Scopes);
