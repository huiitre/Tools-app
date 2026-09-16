using System.Collections.Concurrent;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Polling;

// Un seul poll écrit ici toutes les 10s pendant qu'une lecture peut survenir au même instant
// (une connexion WebSocket qui vient de s'établir) : ConcurrentDictionary plutôt qu'un
// Dictionary, qui n'est pas prévu pour un accès concurrent.
public sealed class InMemoryGameServerLiveStateStore : IGameServerLiveStateStore
{
    private readonly ConcurrentDictionary<string, GameServerLiveSnapshot> snapshotsBySlug = new(StringComparer.Ordinal);

    public void Set(GameServerLiveSnapshot snapshot) => snapshotsBySlug[snapshot.Slug] = snapshot;

    public GameServerLiveSnapshot? Get(string slug) =>
        snapshotsBySlug.TryGetValue(slug, out var snapshot) ? snapshot : null;

    public IReadOnlyList<GameServerLiveSnapshot> GetAll() => snapshotsBySlug.Values.ToList();
}
