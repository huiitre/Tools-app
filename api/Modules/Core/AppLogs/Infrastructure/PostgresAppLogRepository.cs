using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Ports;

namespace Tools.Api.Modules.Core.AppLogs.Infrastructure;

// Adaptateur PostgreSQL/Dapper append-only du journal applicatif.
public sealed class PostgresAppLogRepository(NpgsqlDataSource dataSource) : IAppLogRepository
{
    public async Task<long> InsertAsync(AppLogEntry entry)
    {
        const string sql = """
            INSERT INTO tools_core.application_logs
                (module_id, area_code, action_code, user_id, ip_address, user_agent, metadata)
            VALUES
                (@ModuleId, @AreaCode, @ActionCode, @UserId, @IpAddress, @UserAgent, @Metadata::jsonb)
            RETURNING id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<long>(new CommandDefinition(sql, entry));
    }
}
