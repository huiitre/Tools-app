using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.IntegrationTests.Fakes;

// Hébergeur de mods simulé : il ne reconnaît que les URLs https://mods.example/ et garde chaque
// demande, pour qu'un test vérifie qu'une icône connue n'est pas redemandée.
public sealed class FakeModIconResolver : IModIconResolver
{
    private readonly Dictionary<string, string> icons = new(StringComparer.Ordinal);

    public List<IReadOnlyCollection<string>> Requests { get; } = [];

    public bool Unavailable { get; set; }

    public void Clear()
    {
        icons.Clear();
        Requests.Clear();
        Unavailable = false;
    }

    public void Set(string modUrl, string iconUrl) => icons[modUrl] = iconUrl;

    public bool Supports(string modUrl) => modUrl.StartsWith("https://mods.example/", StringComparison.Ordinal);

    public Task<IReadOnlyDictionary<string, string>> ResolveAsync(IReadOnlyCollection<string> modUrls)
    {
        Requests.Add(modUrls);
        IReadOnlyDictionary<string, string> resolved = Unavailable
            ? new Dictionary<string, string>()
            : modUrls.Where(icons.ContainsKey).ToDictionary(modUrl => modUrl, modUrl => icons[modUrl]);
        return Task.FromResult(resolved);
    }
}
