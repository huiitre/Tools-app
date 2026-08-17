using Dapper;
using Npgsql;
using Tools.Api.Modules.Realtime.Application.Ports;

namespace Tools.Api.Modules.Realtime.Infrastructure;

// Duplique volontairement les requêtes de PostgresNotificationRepository : la résolution
// "qui a ce rôle / qui est membre de ce module" n'est pas un concept de notification, c'est un
// concept transverse aux appelants du Hub — la coupler au module Notifications créerait une
// dépendance sans rapport avec sa responsabilité réelle.
public sealed class PostgresRecipientResolver(NpgsqlDataSource dataSource) : IRecipientResolver
{
    public async Task<bool> UserExistsAsync(long userId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.users WHERE id = @UserId)";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId }));
    }

    public async Task<IReadOnlyList<long>> FindByRoleCodesAsync(IReadOnlyCollection<string> roleCodes)
    {
        const string sql = """
            SELECT DISTINCT ur.user_id
            FROM tools_core.user_role ur
            INNER JOIN tools_core.role r ON r.id = ur.role_id
            INNER JOIN tools_core.users u ON u.id = ur.user_id
            WHERE r.code = ANY(@RoleCodes)
              AND u.is_active
              AND NOT EXISTS (
                  SELECT 1
                  FROM tools_core.user_role tech_role
                  INNER JOIN tools_core.role tech ON tech.id = tech_role.role_id
                  WHERE tech_role.user_id = ur.user_id AND tech.code = 'TECH'
              )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var recipients = await connection.QueryAsync<long>(
            new CommandDefinition(sql, new { RoleCodes = roleCodes.ToArray() }));
        return recipients.ToList();
    }

    public async Task<IReadOnlyList<long>> FindByModuleIdAsync(long moduleId)
    {
        const string sql = """
            SELECT DISTINCT umr.user_id
            FROM tools_core.user_module_role umr
            INNER JOIN tools_core.users u ON u.id = umr.user_id
            WHERE umr.module_id = @ModuleId
              AND u.is_active
              AND NOT EXISTS (
                  SELECT 1
                  FROM tools_core.user_role tech_role
                  INNER JOIN tools_core.role tech ON tech.id = tech_role.role_id
                  WHERE tech_role.user_id = umr.user_id AND tech.code = 'TECH'
              )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var recipients = await connection.QueryAsync<long>(
            new CommandDefinition(sql, new { ModuleId = moduleId }));
        return recipients.ToList();
    }
}
