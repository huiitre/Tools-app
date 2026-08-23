using Dapper;
using Npgsql;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Views;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Domain;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IExpeditionRepository.
//
// Chaque méthode filtre sur user_id en plus de l'identifiant : l'appartenance est vérifiée dans
// le use case ET dans le SQL, pour qu'aucune requête ne puisse toucher la ligne d'un autre.
public sealed class PostgresExpeditionRepository(NpgsqlDataSource dataSource) : IExpeditionRepository
{
    // Classe à propriétés, et non record positionnel : Npgsql annonce `bigint[]` comme
    // `System.Array`, si bien que Dapper ne reconnaît aucun constructeur portant un `long[]`.
    // Affectées une à une, les valeurs passent sans que leur signature ait à correspondre.
    private sealed class DetailRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string RouteData { get; set; } = string.Empty;
        public int CurrentSystemIndex { get; set; }
        public long[] CurrentBodiesDone { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public async Task<List<ExpeditionSummaryView>> FindAllByUserId(long userId)
    {
        // Le nombre d'étapes se compte dans le JSON : pas de colonne à tenir à jour.
        const string sql = """
            SELECT id AS Id, name AS Name, source AS Source,
                   current_system_index AS CurrentSystemIndex,
                   jsonb_array_length(route_data->'result') AS TotalSystems,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tools_elite_dangerous.r2r_expedition
            WHERE user_id = @UserId
            ORDER BY updated_at DESC
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var expeditions = await connection.QueryAsync<ExpeditionSummaryView>(
            new CommandDefinition(sql, new { UserId = userId }));

        return expeditions.ToList();
    }

    public async Task<ExpeditionDetailView?> FindByIdAndUserId(Guid expeditionId, long userId)
    {
        const string sql = """
            SELECT id AS Id, name AS Name, source AS Source, route_data::text AS RouteData,
                   current_system_index AS CurrentSystemIndex,
                   current_bodies_done AS CurrentBodiesDone,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tools_elite_dangerous.r2r_expedition
            WHERE id = @ExpeditionId AND user_id = @UserId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var row = await connection.QueryFirstOrDefaultAsync<DetailRow>(
            new CommandDefinition(sql, new { ExpeditionId = expeditionId, UserId = userId }));

        if (row is null)
        {
            return null;
        }

        return new ExpeditionDetailView(
            row.Id,
            row.Name,
            row.Source,
            row.RouteData,
            row.CurrentSystemIndex,
            [.. row.CurrentBodiesDone],
            row.CreatedAt,
            row.UpdatedAt);
    }

    public async Task<string?> FindRouteDataByIdAndUserId(Guid expeditionId, long userId)
    {
        const string sql = """
            SELECT route_data::text
            FROM tools_elite_dangerous.r2r_expedition
            WHERE id = @ExpeditionId AND user_id = @UserId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { ExpeditionId = expeditionId, UserId = userId }));
    }

    public async Task<Guid> Save(long userId, Expedition expedition)
    {
        const string sql = """
            INSERT INTO tools_elite_dangerous.r2r_expedition (user_id, name, source, route_data)
            VALUES (@UserId, @Name, @Source, @RouteData::jsonb)
            RETURNING id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(sql, new
        {
            UserId = userId,
            expedition.Name,
            expedition.Source,
            expedition.RouteData
        }));
    }

    public async Task UpdateProgress(
        Guid id,
        long userId,
        int currentSystemIndex,
        List<long> currentBodiesDone)
    {
        const string sql = """
            UPDATE tools_elite_dangerous.r2r_expedition
            SET current_system_index = @CurrentSystemIndex,
                current_bodies_done  = @CurrentBodiesDone,
                updated_at           = now()
            WHERE id = @Id AND user_id = @UserId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            CurrentSystemIndex = currentSystemIndex,
            // Npgsql écrit un long[] directement dans un bigint[] : aucun littéral à composer.
            CurrentBodiesDone = currentBodiesDone.ToArray(),
            Id = id,
            UserId = userId
        }));
    }

    public async Task Rename(Guid id, long userId, string name)
    {
        const string sql = """
            UPDATE tools_elite_dangerous.r2r_expedition
            SET name = @Name, updated_at = now()
            WHERE id = @Id AND user_id = @UserId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Name = name, Id = id, UserId = userId }));
    }

    public async Task Delete(Guid id, long userId)
    {
        const string sql = """
            DELETE FROM tools_elite_dangerous.r2r_expedition
            WHERE id = @Id AND user_id = @UserId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, UserId = userId }));
    }

    public async Task<bool> ExistsByIdAndUserId(Guid id, long userId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tools_elite_dangerous.r2r_expedition
                WHERE id = @Id AND user_id = @UserId
            )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }));
    }

    public async Task<int> CountByUserId(long userId)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM tools_elite_dangerous.r2r_expedition
            WHERE user_id = @UserId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { UserId = userId }));
    }
}
