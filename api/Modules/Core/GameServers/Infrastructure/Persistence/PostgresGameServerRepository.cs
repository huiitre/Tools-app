using Dapper;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Polling;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du sync. Toutes ses méthodes exigent la transaction ouverte par
// le use case afin qu'un payload invalide ou une erreur Steam ne puisse jamais provoquer un
// demi-sync suivi de suppressions.
public sealed class PostgresGameServerRepository(
    PostgresSession session,
    Npgsql.NpgsqlDataSource dataSource) : IGameServerRepository, IGameServerPollingRepository, IGameServerDashboardRepository, IGameServerTargetRepository
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
                    game_name, picture_url, host, port, client_host, client_port,
                    protocol_config, modpack_url, modpack_size, last_synced_at)
                VALUES (
                    @Slug, @GameCode, @ProtocolType, @ServerName, @SteamAppId,
                    @GameName, @PictureUrl, @Host, @Port, @ClientHost, @ClientPort,
                    CAST(@ProtocolConfig AS jsonb), @ModpackUrl, @ModpackSize, now())
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
                client_host = @ClientHost,
                client_port = @ClientPort,
                protocol_config = CAST(@ProtocolConfig AS jsonb),
                modpack_url = @ModpackUrl,
                modpack_size = @ModpackSize,
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
                  OR client_host IS DISTINCT FROM @ClientHost
                  OR client_port IS DISTINCT FROM @ClientPort
                  OR protocol_config IS DISTINCT FROM CAST(@ProtocolConfig AS jsonb)
                  OR modpack_url IS DISTINCT FROM @ModpackUrl
                  OR modpack_size IS DISTINCT FROM @ModpackSize
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
                gameServer.ClientHost,
                gameServer.ClientPort,
                gameServer.ProtocolConfig,
                gameServer.ModpackUrl,
                gameServer.ModpackSize
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

    // Une liste modifiée est réécrite en entier : aucun mod n'a d'identité stable d'un export à
    // l'autre (l'URL peut manquer, le nom changer), et rien ne référence ces lignes.
    public async Task<bool> ReplaceModsAsync(string slug, IReadOnlyList<GameServerModEntry> mods)
    {
        var connection = Connection();
        var gameServerId = await connection.QuerySingleAsync<long>(new CommandDefinition(
            "SELECT id FROM tools_core.game_servers WHERE slug = @Slug",
            new { Slug = slug }, session.Transaction));

        var existing = await connection.QueryAsync<ModRow>(new CommandDefinition(
            $"""
            SELECT {ModColumns}
            FROM tools_core.game_server_mods
            WHERE game_server_id = @GameServerId
            ORDER BY position
            """,
            new { GameServerId = gameServerId }, session.Transaction));

        if (existing.Select(row => row.ToEntry()).SequenceEqual(mods))
        {
            return false;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM tools_core.game_server_mods WHERE game_server_id = @GameServerId",
            new { GameServerId = gameServerId }, session.Transaction));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO tools_core.game_server_mods (
                game_server_id, position, name, version, url, authors, file_name, icon_url)
            VALUES (
                @GameServerId, @Position, @Name, @Version, @Url, @Authors, @FileName, @IconUrl)
            """,
            mods.Select((mod, position) => new
            {
                GameServerId = gameServerId,
                Position = position,
                mod.Name,
                mod.Version,
                mod.Url,
                Authors = mod.Authors.ToArray(),
                mod.FileName,
                mod.IconUrl
            }).ToList(),
            session.Transaction));

        return true;
    }

    public Task<int> DeleteMissingAsync(IReadOnlyCollection<string> slugs) => Connection().ExecuteAsync(
        new CommandDefinition(
            """
            DELETE FROM tools_core.game_servers
            WHERE NOT (slug = ANY(CAST(@Slugs AS text[])))
            """,
            new { Slugs = slugs.ToArray() }, session.Transaction));

    public async Task<IReadOnlyDictionary<string, string>> FindModIconsAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var icons = await connection.QueryAsync<(string Url, string IconUrl)>(
            """
            SELECT DISTINCT ON (url) url, icon_url
            FROM tools_core.game_server_mods
            WHERE url IS NOT NULL AND icon_url IS NOT NULL
            ORDER BY url
            """);
        return icons.ToDictionary(icon => icon.Url, icon => icon.IconUrl, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<GameServerTarget>> FindAllForPollingAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var gameServers = await connection.QueryAsync<GameServerTarget>(
            """
            SELECT id AS Id,
                   slug AS Slug,
                   game_code AS GameCode,
                   server_name AS ServerName,
                   game_name AS GameName,
                   picture_url AS PictureUrl,
                   host AS Host,
                   port AS Port,
                   protocol_config::text AS ProtocolConfig
            FROM tools_core.game_servers
            ORDER BY id
            """);
        return gameServers.AsList();
    }

    public async Task<GameServerTarget?> FindBySlugAsync(string slug)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<GameServerTarget>(
            """
            SELECT id AS Id,
                   slug AS Slug,
                   game_code AS GameCode,
                   server_name AS ServerName,
                   game_name AS GameName,
                   picture_url AS PictureUrl,
                   host AS Host,
                   port AS Port,
                   protocol_config::text AS ProtocolConfig
            FROM tools_core.game_servers
            WHERE slug = @Slug AND is_visible
            """,
            new { Slug = slug });
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

    public async Task<IReadOnlyList<GameServerListRow>> FindVisibleForDashboardAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var gameServers = await connection.QueryAsync<GameServerListRow>(
            """
            SELECT slug AS Slug,
                   game_code AS GameCode,
                   COALESCE(game_name, game_code) AS GameName,
                   server_name AS ServerName,
                   picture_url AS PictureUrl,
                   online AS Online,
                   num_players AS NumPlayers,
                   max_players AS MaxPlayers,
                   checked_at AS CheckedAt,
                   client_host AS ClientHost,
                   client_port AS ClientPort,
                   (SELECT count(*)::int
                    FROM tools_core.game_server_mods mods
                    WHERE mods.game_server_id = game_servers.id) AS ModCount,
                   modpack_url IS NOT NULL AS HasModpack
            FROM tools_core.game_servers
            WHERE is_visible = true
            ORDER BY COALESCE(game_name, game_code), server_name
            """);
        return gameServers.AsList();
    }

    public async Task<GameServerModsView?> FindModsBySlugAsync(string slug)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var gameServer = await connection.QuerySingleOrDefaultAsync<ModpackRow>(
            """
            SELECT id AS Id, modpack_url AS ModpackUrl, modpack_size AS ModpackSize
            FROM tools_core.game_servers
            WHERE slug = @Slug AND is_visible
            """,
            new { Slug = slug });
        if (gameServer is null)
        {
            return null;
        }

        var mods = await connection.QueryAsync<ModRow>(
            $"""
            SELECT {ModColumns}
            FROM tools_core.game_server_mods
            WHERE game_server_id = @Id
            ORDER BY position
            """,
            new { gameServer.Id });

        return new GameServerModsView(
            gameServer.ModpackUrl,
            gameServer.ModpackSize,
            mods.Select(row => row.ToView()).ToList());
    }

    private const string ModColumns =
        "name AS Name, version AS Version, url AS Url, authors AS Authors, file_name AS FileName, icon_url AS IconUrl";

    private Npgsql.NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");

    private sealed record ExistingMetadata(string? GameName, string? PictureUrl);

    private sealed record ModpackRow(long Id, string? ModpackUrl, long? ModpackSize);

    // Classe à propriétés et non record : Npgsql annonce text[] comme System.Array, et Dapper ne
    // trouve alors aucun constructeur correspondant.
    private sealed class ModRow
    {
        public string Name { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string? Url { get; set; }
        public string[] Authors { get; set; } = [];
        public string? FileName { get; set; }
        public string? IconUrl { get; set; }

        public GameServerModEntry ToEntry() => new(Name, Version, Url, Authors, FileName, IconUrl);

        public GameServerModView ToView() => new(Name, Version, Url, Authors, FileName, IconUrl);
    }
}
