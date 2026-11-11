using Tools.Api.Modules.Core.Security.Domain;
using System.Net;
using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.ApiKeys.Domain;
using Tools.Api.Modules.Core.Common.Infrastructure;

namespace Tools.Api.Modules.Core.ApiKeys.Infrastructure;

// Lecture sur sa propre connexion, écriture dans la transaction ambiante.
public sealed class PostgresApiKeyRepository(
    NpgsqlDataSource dataSource,
    PostgresSession session) : IApiKeyRepository
{
    // Les scopes viennent bruts : c'est Rehydrate qui isole ceux que le catalogue ne connaît plus.
    private const string SelectApiKeys = """
        SELECT k.id, k.name, k.description, k.key_hash AS KeyHash, k.key_prefix AS KeyPrefix,
               ARRAY(SELECT s.scope FROM tools_core.api_key_scopes s WHERE s.api_key_id = k.id ORDER BY s.scope) AS Scopes,
               k.created_at AS CreatedAt, k.created_by AS CreatedBy, k.expires_at AS ExpiresAt,
               k.revoked_at AS RevokedAt, k.last_used_at AS LastUsedAt, host(k.last_used_ip) AS LastUsedIp
        FROM tools_core.api_keys k
        """;

    public async Task<List<ApiKey>> FindAll()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var rows = await connection.QueryAsync<ApiKeyRow>(new CommandDefinition(
            SelectApiKeys + " ORDER BY k.created_at DESC, k.id DESC"));

        return rows.Select(ToDomain).ToList();
    }

    public async Task<ApiKey?> FindById(long id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var row = await connection.QuerySingleOrDefaultAsync<ApiKeyRow>(new CommandDefinition(
            SelectApiKeys + " WHERE k.id = @Id", new { Id = id }));

        return row is null ? null : ToDomain(row);
    }

    public async Task<ApiKey?> FindByHash(byte[] keyHash)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var row = await connection.QuerySingleOrDefaultAsync<ApiKeyRow>(new CommandDefinition(
            SelectApiKeys + " WHERE k.key_hash = @KeyHash", new { KeyHash = keyHash }));

        return row is null ? null : ToDomain(row);
    }

    public async Task<bool> ActiveNameIsTaken(string name)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tools_core.api_keys WHERE name = @Name AND revoked_at IS NULL
            )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { Name = name }));
    }

    public async Task<long> Create(ApiKey apiKey)
    {
        var id = await Connection().ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO tools_core.api_keys
                (name, description, key_hash, key_prefix, created_at, created_by, expires_at)
            VALUES
                (@Name, @Description, @KeyHash, @KeyPrefix, @CreatedAt, @CreatedBy, @ExpiresAt)
            RETURNING id
            """,
            new
            {
                apiKey.Name,
                apiKey.Description,
                apiKey.KeyHash,
                apiKey.KeyPrefix,
                CreatedAt = apiKey.CreatedAt.ToUniversalTime(),
                apiKey.CreatedBy,
                ExpiresAt = apiKey.ExpiresAt.ToUniversalTime()
            },
            session.Transaction));

        await Connection().ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO tools_core.api_key_scopes (api_key_id, scope)
            SELECT @ApiKeyId, unnest(@Scopes)
            """,
            new { ApiKeyId = id, Scopes = apiKey.Scopes.Select(scope => scope.ToCode()).ToArray() },
            session.Transaction));

        return id;
    }

    public async Task<bool> Revoke(long id, DateTimeOffset revokedAt)
    {
        var affected = await Connection().ExecuteAsync(new CommandDefinition(
            """
            UPDATE tools_core.api_keys
            SET revoked_at = @RevokedAt
            WHERE id = @Id AND revoked_at IS NULL
            """,
            new { Id = id, RevokedAt = revokedAt.ToUniversalTime() },
            session.Transaction));

        return affected == 1;
    }

    // Une écriture par minute et par clé au plus, quel que soit le rythme des appels.
    public async Task TouchLastUsed(long id, IPAddress? ip, TimeSpan throttle)
    {
        const string sql = """
            UPDATE tools_core.api_keys
            SET last_used_at = now(), last_used_ip = CAST(@Ip AS inet)
            WHERE id = @Id
              AND (last_used_at IS NULL OR last_used_at < now() - @Throttle)
            """;

        await using var connection = await dataSource.OpenConnectionAsync();

        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, Ip = ip?.ToString(), Throttle = throttle }));
    }

    private NpgsqlConnection Connection() =>
        session.Connection ?? throw new InvalidOperationException("Écriture hors transaction.");

    private static ApiKey ToDomain(ApiKeyRow row) => ApiKey.Rehydrate(
        row.Id,
        row.Name,
        row.Description,
        row.KeyHash,
        row.KeyPrefix,
        row.Scopes,
        Utc(row.CreatedAt),
        row.CreatedBy,
        Utc(row.ExpiresAt),
        row.RevokedAt is { } revokedAt ? Utc(revokedAt) : null,
        row.LastUsedAt is { } lastUsedAt ? Utc(lastUsedAt) : null,
        row.LastUsedIp is null ? null : IPAddress.Parse(row.LastUsedIp));

    // Npgsql lit un timestamptz en DateTime UTC.
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed class ApiKeyRow
    {
        public long Id { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public byte[] KeyHash { get; init; } = [];
        public string KeyPrefix { get; init; } = "";
        public string[] Scopes { get; init; } = [];
        public DateTime CreatedAt { get; init; }
        public long CreatedBy { get; init; }
        public DateTime ExpiresAt { get; init; }
        public DateTime? RevokedAt { get; init; }
        public DateTime? LastUsedAt { get; init; }
        public string? LastUsedIp { get; init; }
    }
}
