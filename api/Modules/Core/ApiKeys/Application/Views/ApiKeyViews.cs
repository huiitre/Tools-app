using Tools.Api.Modules.Core.ApiKeys.Domain;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Views;

// Ni la clé en clair ni son hash : seul le préfixe permet de la reconnaître.
public sealed record ApiKeyView(
    long Id,
    string Name,
    string Description,
    string KeyPrefix,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> UnknownScopes,
    DateTimeOffset CreatedAt,
    long CreatedBy,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? LastUsedAt,
    string? LastUsedIp,
    bool IsExpired,
    bool IsRevoked)
{
    public static ApiKeyView From(ApiKey apiKey)
    {
        var now = DateTimeOffset.UtcNow;

        return new ApiKeyView(
            apiKey.Id ?? throw new InvalidOperationException("Une clé non enregistrée n'a pas de vue."),
            apiKey.Name,
            apiKey.Description,
            apiKey.KeyPrefix,
            apiKey.Scopes.Select(scope => scope.ToCode()).Order().ToList(),
            apiKey.UnknownScopes.Order().ToList(),
            apiKey.CreatedAt,
            apiKey.CreatedBy,
            apiKey.ExpiresAt,
            apiKey.RevokedAt,
            apiKey.LastUsedAt,
            apiKey.LastUsedIp?.ToString(),
            apiKey.ExpiresAt <= now,
            apiKey.RevokedAt is not null);
    }
}

// Seule réponse qui contienne la clé en clair : elle ne sera plus jamais lisible ensuite.
public sealed record CreatedApiKeyView(ApiKeyView ApiKey, string Key)
{
    // Le ToString généré d'un record afficherait la clé en clair dans un log.
    public override string ToString() => $"CreatedApiKeyView {{ ApiKey = {ApiKey.KeyPrefix}… }}";
}
