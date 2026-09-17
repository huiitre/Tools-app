using Dapper;
using Npgsql;
using System.Text.Json;
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
                (@ModuleId, @AreaCode, @ActionCode, @UserId, CAST(@IpAddress AS inet), @UserAgent, @Metadata::jsonb)
            RETURNING id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        // Dapper ne connaît pas le type .NET IPAddress. Sa représentation canonique est toutefois
        // une valeur PostgreSQL inet valide, y compris en IPv6 ; le cast reste donc dans le SQL.
        return await connection.QuerySingleAsync<long>(new CommandDefinition(sql, new
        {
            entry.ModuleId,
            entry.AreaCode,
            entry.ActionCode,
            entry.UserId,
            IpAddress = entry.IpAddress?.ToString(),
            entry.UserAgent,
            entry.Metadata
        }));
    }

    public async Task<AppLogPageDto> FindForAdminAsync(AppLogListQuery query)
    {
        // La colonne est choisie dans cette liste fermée : une valeur reçue ne devient jamais du SQL.
        var orderBy = query.SortBy switch
        {
            AppLogSortColumn.CreatedAt => "log.created_at",
            AppLogSortColumn.UserName => "user_account.name",
            AppLogSortColumn.UserEmail => "user_account.email",
            AppLogSortColumn.UserRole => "role.code",
            AppLogSortColumn.UserStatus => "user_account.is_active",
            AppLogSortColumn.UserRegisteredAt => "user_account.created_at",
            AppLogSortColumn.ModuleName => "module.name",
            AppLogSortColumn.AreaCode => "log.area_code",
            AppLogSortColumn.ActionCode => "log.action_code",
            AppLogSortColumn.IpAddress => "log.ip_address",
            AppLogSortColumn.UserAgent => "log.user_agent",
            AppLogSortColumn.HasMetadata => "(log.metadata <> jsonb_build_object())",
            _ => throw new ArgumentOutOfRangeException(nameof(query.SortBy))
        };
        var direction = query.SortDirection == SortDirection.Asc ? "ASC" : "DESC";
        var filters = new List<string>
        {
            "(@Search IS NULL OR user_account.name ILIKE '%' || @Search || '%'"
            + " OR user_account.email ILIKE '%' || @Search || '%'"
            + " OR host(log.ip_address) ILIKE '%' || @Search || '%'"
            + " OR log.user_agent ILIKE '%' || @Search || '%')",
            "(@RoleId IS NULL OR user_role.role_id = @RoleId)",
            "(@UserActive IS NULL OR user_account.is_active = @UserActive)",
            "(CAST(@UserRegisteredFrom AS timestamp) IS NULL OR user_account.created_at >= CAST(@UserRegisteredFrom AS timestamp))",
            "(CAST(@UserRegisteredTo AS timestamp) IS NULL OR user_account.created_at < CAST(@UserRegisteredTo AS timestamp) + INTERVAL '1 day')",
            "(@IpAddress IS NULL OR host(log.ip_address) = @IpAddress)",
            "(@HasMetadata IS NULL OR (log.metadata <> jsonb_build_object()) = @HasMetadata)",
            "(CAST(@CreatedFrom AS timestamptz) IS NULL OR log.created_at >= CAST(@CreatedFrom AS timestamptz))",
            "(CAST(@CreatedTo AS timestamptz) IS NULL OR log.created_at < CAST(@CreatedTo AS timestamptz) + INTERVAL '1 day')"
        };
        if (query.UserIds is { Length: > 0 }) filters.Add("log.user_id = ANY(@UserIds)");
        if (query.ModuleIds is { Length: > 0 }) filters.Add("log.module_id = ANY(@ModuleIds)");
        if (query.AreaCodes is { Length: > 0 }) filters.Add("log.area_code = ANY(@AreaCodes)");
        if (query.ActionCodes is { Length: > 0 }) filters.Add("log.action_code = ANY(@ActionCodes)");
        var where = string.Join("\n                  AND ", filters);

        // Les références user/module sont historiques : une jointure gauche conserve le log même
        // quand la ressource d'origine n'existe plus.
        var sql = $"""
            WITH filtered_logs AS (
                SELECT log.id
                FROM tools_core.application_logs log
                LEFT JOIN tools_core.module module ON module.id = log.module_id
                LEFT JOIN tools_core.users user_account ON user_account.id = log.user_id
                LEFT JOIN tools_core.user_role user_role ON user_role.user_id = user_account.id
                LEFT JOIN tools_core.role role ON role.id = user_role.role_id
                WHERE {where}
            )
            SELECT COUNT(*) FROM filtered_logs;

            WITH filtered_logs AS (
                SELECT log.id
                FROM tools_core.application_logs log
                LEFT JOIN tools_core.module module ON module.id = log.module_id
                LEFT JOIN tools_core.users user_account ON user_account.id = log.user_id
                LEFT JOIN tools_core.user_role user_role ON user_role.user_id = user_account.id
                LEFT JOIN tools_core.role role ON role.id = user_role.role_id
                WHERE {where}
            )
            SELECT log.id AS Id, log.created_at AS CreatedAt,
                   log.module_id AS ModuleId, module.name AS ModuleName,
                   log.area_code AS AreaCode, log.action_code AS ActionCode,
                   log.user_id AS UserId,
                   user_account.name AS UserName, user_account.email AS UserEmail,
                   user_role.role_id AS UserRoleId, role.code AS UserRoleCode,
                   user_account.is_active AS UserActive,
                   user_account.created_at AS UserRegisteredAt,
                   host(log.ip_address) AS IpAddress, log.user_agent AS UserAgent,
                   (log.metadata <> jsonb_build_object()) AS HasMetadata,
                   log.metadata::text AS MetadataJson
            FROM tools_core.application_logs log
            LEFT JOIN tools_core.module module ON module.id = log.module_id
            LEFT JOIN tools_core.users user_account ON user_account.id = log.user_id
            LEFT JOIN tools_core.user_role user_role ON user_role.user_id = user_account.id
            LEFT JOIN tools_core.role role ON role.id = user_role.role_id
            WHERE log.id IN (SELECT id FROM filtered_logs)
            ORDER BY {orderBy} {direction} NULLS LAST, log.id DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT DISTINCT area_code FROM tools_core.application_logs ORDER BY area_code;
            SELECT DISTINCT action_code FROM tools_core.application_logs ORDER BY action_code;
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, query));
        var totalCount = await results.ReadSingleAsync<long>();
        var rows = await results.ReadAsync<AppLogAdminRow>();
        var areaCodes = (await results.ReadAsync<string>()).ToList();
        var actionCodes = (await results.ReadAsync<string>()).ToList();
        return new AppLogPageDto(rows.Select(row => new AppLogAdminDto(
            row.Id, row.CreatedAt, row.ModuleId, row.ModuleName, row.AreaCode, row.ActionCode,
            row.UserId, row.UserName, row.UserEmail, row.UserRoleId, row.UserRoleCode, row.UserActive,
            row.UserRegisteredAt, row.IpAddress, row.UserAgent, row.HasMetadata,
            JsonDocument.Parse(row.MetadataJson).RootElement.Clone(), null)).ToList(), totalCount, query.Page, query.PageSize,
            new AppLogFilterOptionsDto(areaCodes, actionCodes));
    }

    private sealed record AppLogAdminRow(
        long Id, DateTime CreatedAt, long? ModuleId, string? ModuleName, string AreaCode, string ActionCode,
        long? UserId, string? UserName, string? UserEmail, long? UserRoleId, string? UserRoleCode,
        bool? UserActive, DateTime? UserRegisteredAt, string? IpAddress, string? UserAgent, bool HasMetadata, string MetadataJson);
}
