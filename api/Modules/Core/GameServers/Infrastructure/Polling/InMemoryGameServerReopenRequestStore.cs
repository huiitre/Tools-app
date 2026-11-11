using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Polling;

// Un verrou plutôt qu'un ConcurrentDictionary seul : « vérifier puis écrire » doit être une seule
// opération, sinon deux clics simultanés passeraient tous les deux.
public sealed class InMemoryGameServerReopenRequestStore : IGameServerReopenRequestStore
{
    private readonly Dictionary<string, DateTime> requestedAtBySlug = new(StringComparer.Ordinal);
    private readonly Lock gate = new();

    public bool TryRequest(string slug, DateTime now, TimeSpan cooldown, out DateTime requestedAt)
    {
        lock (gate)
        {
            if (requestedAtBySlug.TryGetValue(slug, out var previous) && now - previous < cooldown)
            {
                requestedAt = previous;
                return false;
            }

            requestedAtBySlug[slug] = now;
            requestedAt = now;
            return true;
        }
    }

    public DateTime? Get(string slug)
    {
        lock (gate)
        {
            return requestedAtBySlug.TryGetValue(slug, out var requestedAt) ? requestedAt : null;
        }
    }

    public void Clear(string slug)
    {
        lock (gate)
        {
            requestedAtBySlug.Remove(slug);
        }
    }

    public void Cancel(string slug, DateTime requestedAt)
    {
        lock (gate)
        {
            if (requestedAtBySlug.TryGetValue(slug, out var current) && current == requestedAt)
                requestedAtBySlug.Remove(slug);
        }
    }
}
