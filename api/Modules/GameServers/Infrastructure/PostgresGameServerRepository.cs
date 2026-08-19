using Dapper;
using Tools.Api.Modules.Common.Infrastructure;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Infrastructure;

// Adaptateur PostgreSQL/Dapper du sync. Toutes ses méthodes exigent la transaction ouverte par
// le use case afin qu'un payload invalide ou une erreur Steam ne puisse jamais provoquer un
// demi-sync suivi de suppressions.
public sealed class PostgresGameServerRepository(PostgresSession session) : IGameServerRepository
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

    private Npgsql.NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");

    private sealed record ExistingMetadata(string? GameName, string? PictureUrl);
}
