namespace Tools.Api.Modules.Core.Security.Infrastructure;

// Contrat entre l'authentification par clé (module ApiKeys) et sa lecture (HttpCurrentUserProvider).
public static class ApiKeyAuthenticationDefaults
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    // Ancien secret partagé, accepté comme une clé portant tous les scopes le temps de migrer
    // ses appelants ; à retirer ensuite avec INTERNAL_API_TOKEN.
    public const string LegacyHeaderName = "X-Internal-Token";
}

public static class ApiKeyClaims
{
    // Absent pour l'ancien secret partagé, qui n'a pas de ligne en base.
    public const string Id = "apiKeyId";
    public const string Name = "apiKeyName";
    public const string Scope = "apiKeyScope";
}
