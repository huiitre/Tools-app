using System.Net;
using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.ApiKeys.Domain;
using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Services;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Services;

// Valide la clé présentée et trace chaque passage, accepté ou refusé, dans le journal applicatif.
// Ni la clé ni son hash n'y sont écrits : seulement l'id, le nom ou le préfixe visible.
public sealed class ApiKeyAuthenticator(
    IApiKeyRepository apiKeyRepository,
    AppLogService appLogService,
    ILogger<ApiKeyAuthenticator> logger)
{
    public const string LogArea = "API_KEY";
    private static readonly TimeSpan LastUsedThrottle = TimeSpan.FromMinutes(1);
    private const int PresentedPrefixLength = 12;

    // Le passage d'une clé acceptée n'est pas journalisé ici : seul le code de la réponse dit si
    // elle a servi ou tenté une action hors de ses scopes (voir LogUse).
    public async Task<ApiKey?> Authenticate(string presentedKey, IPAddress? ip, string method, string path)
    {
        var apiKey = await apiKeyRepository.FindByHash(ApiKeySecret.HashOf(presentedKey));

        var rejection = apiKey switch
        {
            null => "UNKNOWN",
            { RevokedAt: not null } => "REVOKED",
            _ when !apiKey.IsValid() => "EXPIRED",
            _ => null
        };

        if (rejection is not null)
        {
            logger.LogWarning(
                "Clé d'API refusée reason={Reason} apiKeyId={ApiKeyId} path={Path}", rejection, apiKey?.Id, path);

            await appLogService.Log(new AppLogCommand(null, LogArea, "REJECTED", Metadata: new
            {
                Reason = rejection,
                ApiKeyId = apiKey?.Id,
                ApiKeyName = apiKey?.Name,
                // Seul le début, déjà affiché en clair dans l'administration, pour reconnaître la clé.
                KeyPrefix = presentedKey.Length >= PresentedPrefixLength ? presentedKey[..PresentedPrefixLength] : null,
                Method = method,
                Path = path
            }));

            return null;
        }

        await apiKeyRepository.TouchLastUsed(apiKey!.Id!.Value, ip, LastUsedThrottle);
        return apiKey;
    }

    // Appelé au départ de la réponse ; un succès n'est pas journalisé, `last_used_at` en garde la trace.
    // `apiKey` nul désigne l'ancien secret partagé, journalisé à chaque usage tant qu'il existe.
    public Task LogUse(ApiKey? apiKey, string method, string path, int status) =>
        apiKey is not null && status < 400 ? Task.CompletedTask : appLogService.Log(new AppLogCommand(null, LogArea, apiKey is null ? "LEGACY_USED" : "USED", Metadata: new
        {
            ApiKeyId = apiKey?.Id,
            ApiKeyName = apiKey?.Name,
            Method = method,
            Path = path,
            Status = status
        }));

    public Task LogLegacyRejected(string method, string path) =>
        appLogService.Log(new AppLogCommand(null, LogArea, "REJECTED", Metadata: new
        {
            Reason = "LEGACY_TOKEN_MISMATCH",
            Method = method,
            Path = path
        }));
}
