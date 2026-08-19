using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class InMemoryGameServerRepository : IGameServerRepository
{
    private readonly Dictionary<string, StoredGameServer> gameServers = new(StringComparer.Ordinal);

    public IReadOnlyCollection<StoredGameServer> GameServers => gameServers.Values;

    public void Clear() => gameServers.Clear();

    public Task<GameServerUpsertResult> UpsertAsync(GameServerSyncEntry gameServer)
    {
        if (!gameServers.TryGetValue(gameServer.Slug, out var existing))
        {
            gameServers.Add(gameServer.Slug, StoredGameServer.From(gameServer));
            return Task.FromResult(GameServerUpsertResult.Created);
        }

        var candidate = StoredGameServer.From(gameServer, existing);
        if (candidate == existing)
        {
            return Task.FromResult(GameServerUpsertResult.Unchanged);
        }

        gameServers[gameServer.Slug] = candidate;
        return Task.FromResult(GameServerUpsertResult.Updated);
    }

    public Task<int> DeleteMissingAsync(IReadOnlyCollection<string> slugs)
    {
        var missing = gameServers.Keys.Where(slug => !slugs.Contains(slug)).ToArray();
        foreach (var slug in missing)
        {
            gameServers.Remove(slug);
        }

        return Task.FromResult(missing.Length);
    }
}

public sealed record StoredGameServer(
    string Slug,
    string GameCode,
    string ProtocolType,
    string ServerName,
    int? SteamAppId,
    string? GameName,
    string? PictureUrl,
    string Host,
    int Port,
    string ProtocolConfig)
{
    public static StoredGameServer From(GameServerSyncEntry entry, StoredGameServer? existing = null) => new(
        entry.Slug,
        entry.GameCode,
        entry.ProtocolType,
        entry.ServerName,
        entry.SteamAppId,
        entry.SteamMetadataAvailable ? entry.GameName : existing?.GameName,
        entry.HasLocalPicture
            ? entry.PictureUrl
            : entry.SteamMetadataAvailable ? entry.PictureUrl : existing?.PictureUrl,
        entry.Host,
        entry.Port,
        entry.ProtocolConfig);
}
