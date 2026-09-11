using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Sync;

// Modrinth, sans clé : un seul appel pour tous les mods. L'URL d'une page de mod porte son
// identifiant ou son slug, que /v2/projects accepte indifféremment.
public sealed class ModrinthIconResolver(
    HttpClient httpClient,
    ILogger<ModrinthIconResolver> logger) : IModIconResolver
{
    private static readonly HashSet<string> ProjectTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "mod", "plugin", "datapack", "resourcepack", "shader", "modpack", "project"
    };

    public bool Supports(string modUrl) => ProjectIdOf(modUrl) is not null;

    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(IReadOnlyCollection<string> modUrls)
    {
        var projectIdsByUrl = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var modUrl in modUrls)
        {
            if (ProjectIdOf(modUrl) is { } projectId)
            {
                projectIdsByUrl[modUrl] = projectId;
            }
        }

        if (projectIdsByUrl.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var ids = JsonSerializer.Serialize(projectIdsByUrl.Values.Distinct(StringComparer.OrdinalIgnoreCase));
        try
        {
            using var response = await httpClient.GetAsync($"v2/projects?ids={Uri.EscapeDataString(ids)}");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Modrinth a refusé la recherche d'icônes avec HTTP {StatusCode}.", (int)response.StatusCode);
                return new Dictionary<string, string>();
            }

            var projects = await response.Content.ReadFromJsonAsync<List<ModrinthProject>>() ?? [];
            var iconsByProject = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in projects.Where(project => !string.IsNullOrWhiteSpace(project.IconUrl)))
            {
                iconsByProject[project.Id] = project.IconUrl!;
                iconsByProject[project.Slug] = project.IconUrl!;
            }

            var icons = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (modUrl, projectId) in projectIdsByUrl)
            {
                if (iconsByProject.TryGetValue(projectId, out var iconUrl))
                {
                    icons[modUrl] = iconUrl;
                }
            }

            return icons;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(exception, "Modrinth est indisponible, les icônes de {Count} mod(s) restent vides.", projectIdsByUrl.Count);
            return new Dictionary<string, string>();
        }
    }

    private static string? ProjectIdOf(string modUrl)
    {
        if (!Uri.TryCreate(modUrl, UriKind.Absolute, out var uri)
            || uri.Host is not ("modrinth.com" or "www.modrinth.com"))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 && ProjectTypes.Contains(segments[0]) ? segments[1] : null;
    }

    private sealed record ModrinthProject(
        string Id,
        string Slug,
        [property: JsonPropertyName("icon_url")] string? IconUrl);
}
