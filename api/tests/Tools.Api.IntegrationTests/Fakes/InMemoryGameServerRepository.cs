using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Polling;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class InMemoryGameServerRepository : IGameServerRepository, IGameServerPollingRepository, IGameServerDashboardRepository, IGameServerTargetRepository
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

    public Task<IReadOnlyList<GameServerTarget>> FindAllForPollingAsync()
    {
        IReadOnlyList<GameServerTarget> targets = gameServers.Values
            .Select((gameServer, index) => new GameServerTarget(
                index + 1,
                gameServer.Slug,
                gameServer.GameCode,
                gameServer.ServerName,
                gameServer.GameName,
                gameServer.PictureUrl,
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

    public Task<GameServerTarget?> FindBySlugAsync(string slug)
    {
        if (!gameServers.TryGetValue(slug, out var gameServer))
        {
            return Task.FromResult<GameServerTarget?>(null);
        }

        return Task.FromResult<GameServerTarget?>(new GameServerTarget(
            1,
            gameServer.Slug,
            gameServer.GameCode,
            gameServer.ServerName,
            gameServer.GameName,
            gameServer.PictureUrl,
            gameServer.Host,
            gameServer.Port,
            gameServer.ProtocolConfig));
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
