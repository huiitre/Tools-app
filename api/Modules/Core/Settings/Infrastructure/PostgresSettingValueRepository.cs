using System.Text.Json.Nodes;
using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Settings.Application.Ports;
using Tools.Api.Modules.Core.Settings.Domain;

namespace Tools.Api.Modules.Core.Settings.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port ISettingValueRepository.
public sealed class PostgresSettingValueRepository(NpgsqlDataSource dataSource) : ISettingValueRepository
{
    public async Task<IReadOnlyList<SettingValue>> FindAsync(
        IReadOnlyCollection<string> codes,
        long? userId,
        IReadOnlyCollection<string> roleCodes)
    {
        if (codes.Count == 0)
        {
            return [];
        }

        // Les trois accroches en une requête. Les branches Role et User s'annulent d'elles-mêmes
        // quand l'appelant n'a ni rôle ni identifiant — une tâche de fond ne ramène que le
        // global sans que la requête ait à changer de forme.
        const string sql = """
            SELECT code AS Code, scope AS Scope, role_code AS RoleCode,
                   user_id AS UserId, value AS Value, is_locked AS IsLocked
            FROM tools_core.setting_value
            WHERE code = ANY(@Codes)
              AND (
                    scope = 'GLOBAL'
                 OR (scope = 'ROLE' AND role_code = ANY(@RoleCodes))
                 OR (scope = 'USER' AND user_id = @UserId)
              )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<SettingValueRow>(new CommandDefinition(
            sql,
            new
            {
                Codes = codes.ToArray(),
                RoleCodes = roleCodes.ToArray(),
                UserId = userId
            }));

        return [.. rows.Select(Map).OfType<SettingValue>()];
    }

    // Une ligne illisible est écartée, jamais propagée. `value` est du JSONB valide par
    // construction, mais `scope` et `role_code` sont des chaînes : un code que l'énumération ne
    // connaît pas ne doit pas faire échouer la lecture de tous les autres paramètres.
    private static SettingValue? Map(SettingValueRow row)
    {
        var scope = row.Scope switch
        {
            "GLOBAL" => SettingScope.Global,
            "ROLE" => SettingScope.Role,
            "USER" => SettingScope.User,
            _ => (SettingScope?)null
        };

        if (scope is null)
        {
            return null;
        }

        var role = row.RoleCode is null ? null : RoleCodes.Parse(row.RoleCode);
        if (scope == SettingScope.Role && role is null)
        {
            return null;
        }

        var value = JsonNode.Parse(row.Value);
        return value is null
            ? null
            : new SettingValue(row.Code, scope.Value, role, row.UserId, value, row.IsLocked);
    }

    private sealed record SettingValueRow(
        string Code,
        string Scope,
        string? RoleCode,
        long? UserId,
        string Value,
        bool IsLocked);
}
