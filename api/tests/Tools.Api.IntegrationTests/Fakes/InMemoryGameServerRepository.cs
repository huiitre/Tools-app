using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class InMemoryGameServerRepository : IGameServerRepository, IGameServerPollingRepository, IGameServerDashboardRepository
{
    private readonly Dictionary<string, StoredGameServer> gameServers = new(StringComparer.Ordinal);
    private readonly Dictionary<long, GameServerStatus> statuses = [];

    public IReadOnlyCollection<StoredGameServer> GameServers => gameServers.Values;
    public IReadOnlyDictionary<long, GameServerStatus> Statuses => statuses;

    public void Clear()
    {
        gameServers.Clear();
        statuses.Clear();
    }

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

    public Task<IReadOnlyList<GameServerPollTarget>> FindAllForPollingAsync()
    {
        IReadOnlyList<GameServerPollTarget> targets = gameServers.Values
            .Select((gameServer, index) => new GameServerPollTarget(
                index + 1,
                gameServer.ProtocolType,
                gameServer.Host,
                gameServer.Port,
                gameServer.ProtocolConfig))
            .ToArray();
        return Task.FromResult(targets);
    }

    public Task UpdateStatusAsync(long id, GameServerStatus status)
    {
        statuses[id] = status;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GameServerDashboardView>> FindVisibleForDashboardAsync()
    {
        IReadOnlyList<GameServerDashboardView> views = gameServers.Values
            .Select((gameServer, index) =>
            {
                statuses.TryGetValue(index + 1, out var status);
                return new GameServerDashboardView(
                    gameServer.GameName ?? gameServer.GameCode,
                    gameServer.ServerName,
                    gameServer.PictureUrl,
                    status?.Online,
                    status?.NumPlayers,
                    status?.MaxPlayers,
                    status is null ? null : DateTime.UtcNow,
                    gameServer.ClientHost,
                    gameServer.ClientPort);
            })
            .ToArray();
        return Task.FromResult(views);
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
    string ClientHost,
    int ClientPort,
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
        entry.ClientHost,
        entry.ClientPort,
        entry.ProtocolConfig);
}
