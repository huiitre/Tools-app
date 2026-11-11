namespace Tools.Api.Modules.Core.ApiKeys.Domain;

// Droits qu'une clé d'API peut recevoir ; un use case n'est ouvert à une clé que s'il en déclare un.
public enum ApiKeyScope
{
    CoreMailSend,
    CoreNotificationsPublish,
    CoreGameServersSync,
    RiotValorantSync,
    TemtemSync
}

public static class ApiKeyScopes
{
    // Codes stockés dans tools_core.api_key_scopes ; un code publié ne se renomme jamais.
    private static readonly Dictionary<string, ApiKeyScope> ByCode = new(StringComparer.Ordinal)
    {
        ["core.mail:send"] = ApiKeyScope.CoreMailSend,
        ["core.notifications:publish"] = ApiKeyScope.CoreNotificationsPublish,
        ["core.gameservers:sync"] = ApiKeyScope.CoreGameServersSync,
        ["riot.valorant:sync"] = ApiKeyScope.RiotValorantSync,
        ["temtem:sync"] = ApiKeyScope.TemtemSync
    };

    // Un code inconnu ne vaut jamais un droit.
    public static ApiKeyScope? Parse(string? code) =>
        code is not null && ByCode.TryGetValue(code, out var scope) ? scope : null;

    public static string ToCode(this ApiKeyScope scope) =>
        ByCode.First(entry => entry.Value == scope).Key;

    public static IReadOnlyCollection<string> AllCodes => ByCode.Keys;
}
