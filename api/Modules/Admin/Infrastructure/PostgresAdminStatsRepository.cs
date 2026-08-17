using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Admin.Application.Dto;
using Tools.ApiCore.Modules.Admin.Application.Ports;

namespace Tools.ApiCore.Modules.Admin.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IAdminStatsRepository.
public sealed class PostgresAdminStatsRepository(NpgsqlDataSource dataSource) : IAdminStatsRepository
{
    public async Task<AdminStatsDto> GetStatsAsync()
    {
        // Deux résultats en un aller-retour. L'API Java lance trois requêtes séparées ; le
        // décompte hebdomadaire tient dans la première, la répartition par module porte sur
        // d'autres tables et reste distincte.
        const string sql = """
            SELECT COUNT(*) AS TotalUsers,
                   COUNT(*) FILTER (WHERE is_active) AS ActiveUsers,
                   COUNT(*) FILTER (WHERE created_at >= now() - interval '7 days') AS NewUsersThisWeek
            FROM tools_core.users;

            SELECT m.code AS ModuleCode, m.name AS ModuleName,
                   COUNT(DISTINCT umr.user_id) AS UserCount
            FROM tools_core.module m
            LEFT JOIN tools_core.user_module_role umr ON umr.module_id = m.id
            GROUP BY m.id, m.code, m.name
            ORDER BY UserCount DESC, m.name;
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql));

        var counts = await results.ReadSingleAsync<UserCountsRow>();
        var perModule = (await results.ReadAsync<ModuleUserCountDto>()).ToList();

        return new AdminStatsDto(
            counts.TotalUsers,
            counts.ActiveUsers,
            counts.NewUsersThisWeek,
            perModule);
    }

    private sealed record UserCountsRow(long TotalUsers, long ActiveUsers, long NewUsersThisWeek);
}
