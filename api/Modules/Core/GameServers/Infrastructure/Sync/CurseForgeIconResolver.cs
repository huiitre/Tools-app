using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Sync;

// CurseForge refuse tout appel anonyme : la clé est posée sur le client HTTP à la composition.
// Seules les URLs /projects/<id> sont reconnues — celles des exports Prism, et les seules qui
// portent l'identifiant numérique qu'attend l'API.
public sealed class CurseForgeIconResolver(
    HttpClient httpClient,
    ILogger<CurseForgeIconResolver> logger) : IModIconResolver
{
    public bool Supports(string modUrl) => ModIdOf(modUrl) is not null;

    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(IReadOnlyCollection<string> modUrls)
    {
        var modIdsByUrl = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var modUrl in modUrls)
        {
            if (ModIdOf(modUrl) is { } modId)
            {
                modIdsByUrl[modUrl] = modId;
            }
        }

        if (modIdsByUrl.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        try
        {
            using var response = await httpClient.PostAsJsonAsync("v1/mods", new { modIds = modIdsByUrl.Values.Distinct() });
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("CurseForge a refusé la recherche d'icônes avec HTTP {StatusCode}.", (int)response.StatusCode);
                return new Dictionary<string, string>();
            }

            var payload = await response.Content.ReadFromJsonAsync<CurseForgeModsResponse>();
            var iconsByMod = new Dictionary<long, string>();
            foreach (var mod in payload?.Data ?? [])
            {
                if (!string.IsNullOrWhiteSpace(mod.Logo?.ThumbnailUrl))
                {
                    iconsByMod[mod.Id] = mod.Logo.ThumbnailUrl;
                }
            }

            var icons = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (modUrl, modId) in modIdsByUrl)
            {
                if (iconsByMod.TryGetValue(modId, out var iconUrl))
                {
                    icons[modUrl] = iconUrl;
                }
            }

            return icons;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(exception, "CurseForge est indisponible, les icônes de {Count} mod(s) restent vides.", modIdsByUrl.Count);
            return new Dictionary<string, string>();
        }
    }

    private static long? ModIdOf(string modUrl)
    {
        if (!Uri.TryCreate(modUrl, UriKind.Absolute, out var uri)
            || uri.Host is not ("curseforge.com" or "www.curseforge.com"))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is ["projects", var id]
               && long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var modId)
            ? modId
            : null;
    }

    private sealed record CurseForgeModsResponse(IReadOnlyList<CurseForgeMod>? Data);

    private sealed record CurseForgeMod(long Id, CurseForgeLogo? Logo);

    private sealed record CurseForgeLogo(string? ThumbnailUrl);
}
