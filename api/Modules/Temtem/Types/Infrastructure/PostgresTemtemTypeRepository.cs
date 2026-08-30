using Dapper;
using Npgsql;
using Tools.Api.Modules.Temtem.Types.Application.Ports;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Types.Infrastructure;

// Adaptateur PostgreSQL/Dapper du référentiel des types.
//
// Le catalogue est un référentiel en lecture seule, rechargé par la synchronisation : aucune de
// ces requêtes ne participe à une transaction, elles ouvrent leur propre connexion.
public sealed class PostgresTemtemTypeRepository(NpgsqlDataSource dataSource) : ITemtemTypeRepository
{
    public async Task<List<TemtemTypeView>> FindAll()
    {
        const string sql = """
            SELECT id AS Id, slug AS Slug, name AS Name, image_url AS ImageUrl
            FROM tools_temtem.type
            ORDER BY name
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var types = await connection.QueryAsync<TemtemTypeView>(new CommandDefinition(sql));

        return types.ToList();
    }

    public async Task<Dictionary<(int Attacker, int Defender), decimal>> FindEffectivenessMatrix()
    {
        const string sql = """
            SELECT attacker_type_id AS AttackerTypeId,
                   defender_type_id AS DefenderTypeId,
                   multiplier AS Multiplier
            FROM tools_temtem.type_matrix
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<MatrixRow>(new CommandDefinition(sql));

        return rows.ToDictionary(row => (row.AttackerTypeId, row.DefenderTypeId), row => row.Multiplier);
    }

    private sealed record MatrixRow(int AttackerTypeId, int DefenderTypeId, decimal Multiplier);
}
