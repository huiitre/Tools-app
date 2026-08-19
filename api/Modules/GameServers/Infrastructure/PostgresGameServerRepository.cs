using Dapper;
using Tools.Api.Modules.Common.Infrastructure;
using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Infrastructure;

// Adaptateur PostgreSQL/Dapper du sync. Toutes ses méthodes exigent la transaction ouverte par
// le use case afin qu'un payload invalide ou une erreur Steam ne puisse jamais provoquer un
// demi-sync suivi de suppressions.
public sealed class PostgresGameServerRepository(
    PostgresSession session,
    Npgsql.NpgsqlDataSource dataSource) : IGameServerRepository, IGameServerPollingRepository, IGameServerDashboardRepository
{
    public async Task<GameServerUpsertResult> UpsertAsync(GameServerSyncEntry gameServer)
    {
        var connection = Connection();
        var existing = await connection.QuerySingleOrDefaultAsync<ExistingMetadata>(new CommandDefinition(
            """
            SELECT game_name AS GameName, picture_url AS PictureUrl
            FROM tools_core.game_servers
            WHERE slug = @Slug
            FOR UPDATE
            """,
            new { gameServer.Slug }, session.Transaction));

        if (existing is null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO tools_core.game_servers (
                    slug, game_code, protocol_type, server_name, steam_app_id,
                    game_name, picture_url, host, port, protocol_config, last_synced_at)
                VALUES (
                    @Slug, @GameCode, @ProtocolType, @ServerName, @SteamAppId,
                    @GameName, @PictureUrl, @Host, @Port, CAST(@ProtocolConfig AS jsonb), now())
                """,
                gameServer, session.Transaction));
            return GameServerUpsertResult.Created;
        }

        var gameName = gameServer.SteamMetadataAvailable ? gameServer.GameName : existing.GameName;
        var pictureUrl = gameServer.HasLocalPicture
            ? gameServer.PictureUrl
            : gameServer.SteamMetadataAvailable ? gameServer.PictureUrl : existing.PictureUrl;

        var changed = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE tools_core.game_servers
            SET game_code = @GameCode,
                protocol_type = @ProtocolType,
                server_name = @ServerName,
                steam_app_id = @SteamAppId,
                game_name = @GameName,
                picture_url = @PictureUrl,
                host = @Host,
                port = @Port,
                protocol_config = CAST(@ProtocolConfig AS jsonb),
                last_synced_at = now()
            WHERE slug = @Slug
              AND (
                  game_code IS DISTINCT FROM @GameCode
                  OR protocol_type IS DISTINCT FROM @ProtocolType
                  OR server_name IS DISTINCT FROM @ServerName
                  OR steam_app_id IS DISTINCT FROM @SteamAppId
                  OR game_name IS DISTINCT FROM @GameName
                  OR picture_url IS DISTINCT FROM @PictureUrl
                  OR host IS DISTINCT FROM @Host
                  OR port IS DISTINCT FROM @Port
                  OR protocol_config IS DISTINCT FROM CAST(@ProtocolConfig AS jsonb)
              )
            """,
            new
            {
                gameServer.Slug,
                gameServer.GameCode,
                gameServer.ProtocolType,
                gameServer.ServerName,
                gameServer.SteamAppId,
                GameName = gameName,
                PictureUrl = pictureUrl,
                gameServer.Host,
                gameServer.Port,
                gameServer.ProtocolConfig
            }, session.Transaction));

        if (changed == 1)
        {
            return GameServerUpsertResult.Updated;
        }

        // last_synced_at représente la réception du manifest, même quand la configuration est
        // inchangée. Il est volontairement exclu du calcul Updated/Unchanged.
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE tools_core.game_servers SET last_synced_at = now() WHERE slug = @Slug",
            new { gameServer.Slug }, session.Transaction));
        return GameServerUpsertResult.Unchanged;
    }

    public Task<int> DeleteMissingAsync(IReadOnlyCollection<string> slugs) => Connection().ExecuteAsync(
        new CommandDefinition(
            """
            DELETE FROM tools_core.game_servers
            WHERE NOT (slug = ANY(CAST(@Slugs AS text[])))
            """,
            new { Slugs = slugs.ToArray() }, session.Transaction));

    public async Task<IReadOnlyList<GameServerPollTarget>> FindAllForPollingAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var gameServers = await connection.QueryAsync<GameServerPollTarget>(
            """
            SELECT id AS Id,
                   protocol_type AS ProtocolType,
                   host AS Host,
                   port AS Port,
                   protocol_config::text AS ProtocolConfig
            FROM tools_core.game_servers
            ORDER BY id
            """);
        return gameServers.AsList();
    }

    public async Task UpdateStatusAsync(long id, GameServerStatus status)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            UPDATE tools_core.game_servers
            SET online = @Online,
                num_players = @NumPlayers,
                max_players = @MaxPlayers,
                checked_at = now()
            WHERE id = @Id
            """,
            new { Id = id, status.Online, status.NumPlayers, status.MaxPlayers });
    }

    public async Task<IReadOnlyList<GameServerDashboardView>> FindVisibleForDashboardAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var gameServers = await connection.QueryAsync<GameServerDashboardView>(
            """
            SELECT COALESCE(game_name, game_code) AS GameName,
                   server_name AS ServerName,
                   picture_url AS PictureUrl,
                   online AS Online,
                   num_players AS NumPlayers,
                   max_players AS MaxPlayers,
                   checked_at AS CheckedAt
            FROM tools_core.game_servers
            WHERE is_visible = true
            ORDER BY COALESCE(game_name, game_code), server_name
            """);
        return gameServers.AsList();
    }

    private Npgsql.NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");

    private sealed record ExistingMetadata(string? GameName, string? PictureUrl);
}
