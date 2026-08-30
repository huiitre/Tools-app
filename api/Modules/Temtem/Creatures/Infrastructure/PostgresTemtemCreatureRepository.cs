using Dapper;
using Npgsql;
using Tools.Api.Modules.Temtem.Creatures.Application.Ports;
using Tools.Api.Modules.Temtem.Creatures.Application.Views;
using Tools.Api.Modules.Temtem.Techniques.Infrastructure;
using Tools.Api.Modules.Temtem.Traits.Application.Views;

namespace Tools.Api.Modules.Temtem.Creatures.Infrastructure;

// Adaptateur PostgreSQL/Dapper du catalogue des Temtem.
//
// Les types sont joints plutôt que relus à part : une carte de catalogue est inutilisable sans
// eux, et la liste entière tient en 165 lignes. La fiche coûte trois requêtes — le Temtem, ses
// techniques, ses traits — et non une par technique.
public sealed class PostgresTemtemCreatureRepository(NpgsqlDataSource dataSource) : ITemtemCreatureRepository
{
    public async Task<List<TemtemSummaryView>> FindAll()
    {
        // Par numéro de Temtemdex : c'est l'ordre du jeu, et celui qu'attend la grille.
        var sql = $"SELECT {TemtemCreatureSql.Columns} {TemtemCreatureSql.From} ORDER BY t.id";

        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<TemtemCreatureSql.Row>(new CommandDefinition(sql));

        return rows.Select(TemtemCreatureSql.ToView).ToList();
    }

    public async Task<TemtemDetailView?> FindBySlug(string slug)
    {
        var summarySql = $"SELECT {TemtemCreatureSql.Columns} {TemtemCreatureSql.From} WHERE t.slug = @Slug";

        await using var connection = await dataSource.OpenConnectionAsync();
        var summaryRow = await connection.QueryFirstOrDefaultAsync<TemtemCreatureSql.Row>(
            new CommandDefinition(summarySql, new { Slug = slug }));

        if (summaryRow is null)
        {
            return null;
        }

        // Les techniques apprises par niveau d'abord, dans l'ordre où le joueur les obtient ;
        // l'élevage et l'entraînement ensuite, par nom.
        var techniquesSql = $"""
            SELECT {TemtemTechniqueSql.Columns}, lt.source AS Source, lt.level AS Level
            FROM tools_temtem.temtem_technique lt
            JOIN tools_temtem.technique tec ON tec.id = lt.technique_id
            {TemtemTechniqueSql.Joins}
            WHERE lt.temtem_id = @TemtemId
            ORDER BY lt.level NULLS LAST, tec.name
            """;

        const string traitsSql = """
            SELECT tr.id AS Id, tr.slug AS Slug, tr.name AS Name, tr.effect AS Effect
            FROM tools_temtem.temtem_trait tt
            JOIN tools_temtem.trait tr ON tr.id = tt.trait_id
            WHERE tt.temtem_id = @TemtemId
            ORDER BY tr.name
            """;

        var techniqueRows = await connection.QueryAsync<LearnedRow>(
            new CommandDefinition(techniquesSql, new { TemtemId = summaryRow.Id }));

        var traits = await connection.QueryAsync<TemtemTraitView>(
            new CommandDefinition(traitsSql, new { TemtemId = summaryRow.Id }));

        return new TemtemDetailView(
            TemtemCreatureSql.ToView(summaryRow),
            techniqueRows
                .Select(row => new TemtemLearnedTechniqueView(
                    TemtemTechniqueSql.ToView(row), row.Source, row.Level))
                .ToList(),
            traits.ToList());
    }

    public async Task<bool> Exists(int temtemId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_temtem.temtem WHERE id = @TemtemId)";

        await using var connection = await dataSource.OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { TemtemId = temtemId }));
    }

    // Les identifiants seuls : c'est une vérification, pas un affichage. Un Temtem qui apprend
    // la même technique par deux moyens ne la rend qu'une fois.
    public async Task<HashSet<int>> FindLearnedTechniqueIds(int temtemId)
    {
        const string sql = """
            SELECT DISTINCT technique_id
            FROM tools_temtem.temtem_technique
            WHERE temtem_id = @TemtemId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var ids = await connection.QueryAsync<int>(new CommandDefinition(sql, new { TemtemId = temtemId }));

        return [.. ids];
    }

    // La technique, plus la façon dont ce Temtem l'apprend.
    private sealed class LearnedRow : TemtemTechniqueSql.Row
    {
        public string Source { get; set; } = string.Empty;
        public int? Level { get; set; }
    }
}
