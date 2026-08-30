using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Temtem.Creatures.Infrastructure;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;
using Tools.Api.Modules.Temtem.Techniques.Application.Views;
using Tools.Api.Modules.Temtem.Techniques.Infrastructure;

namespace Tools.Api.Modules.Temtem.Teams.Infrastructure;

// Adaptateur PostgreSQL/Dapper des équipes.
//
// La lecture ouvre sa propre connexion ; l'écriture emprunte la transaction ambiante, une
// composition ne devant jamais rester à moitié écrite. Les deux se lisent au premier coup d'œil :
// `dataSource.OpenConnectionAsync()` d'un côté, `Connection()` de l'autre.
//
// Une équipe se lit en trois requêtes — les équipes, les membres, les techniques retenues — et
// non une par membre. Une seule requête à plat multiplierait chaque membre par ses techniques.
public sealed class PostgresTemtemTeamRepository(
    NpgsqlDataSource dataSource,
    PostgresSession session) : ITemtemTeamRepository
{
    public Task<List<TemtemTeamView>> FindAllByUserId(long userId) =>
        Load("WHERE tm.user_id = @UserId", new { UserId = userId });

    public async Task<TemtemTeamView?> FindByIdAndUserId(long teamId, long userId)
    {
        var teams = await Load(
            "WHERE tm.id = @TeamId AND tm.user_id = @UserId",
            new { TeamId = teamId, UserId = userId });

        return teams.FirstOrDefault();
    }

    public async Task<bool> ExistsForUser(long teamId, long userId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tools_temtem.team WHERE id = @TeamId AND user_id = @UserId
            )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { TeamId = teamId, UserId = userId }));
    }

    // Insensible à la casse et aux espaces de bord : « Team A » et « team a  » désignent la même
    // équipe pour un humain, et la popup les afficherait côte à côte sans les distinguer.
    public async Task<bool> NameIsTaken(long userId, string name, long? exceptTeamId = null)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tools_temtem.team
                WHERE user_id = @UserId
                  AND lower(btrim(name)) = lower(btrim(@Name))
                  AND (@ExceptTeamId::bigint IS NULL OR id <> @ExceptTeamId)
            )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { UserId = userId, Name = name, ExceptTeamId = exceptTeamId }));
    }

    public Task<long> Create(long userId, string name) =>
        Connection().ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO tools_temtem.team (user_id, name)
            VALUES (@UserId, @Name)
            RETURNING id
            """,
            new { UserId = userId, Name = name },
            session.Transaction));

    public async Task<bool> Rename(long teamId, long userId, string name) =>
        await Connection().ExecuteAsync(new CommandDefinition(
            """
            UPDATE tools_temtem.team
            SET name = @Name, updated_at = now()
            WHERE id = @TeamId AND user_id = @UserId
            """,
            new { TeamId = teamId, UserId = userId, Name = name },
            session.Transaction)) > 0;

    // Les membres et leurs techniques partent en cascade : rien à supprimer à la main.
    public async Task<bool> Delete(long teamId, long userId) =>
        await Connection().ExecuteAsync(new CommandDefinition(
            "DELETE FROM tools_temtem.team WHERE id = @TeamId AND user_id = @UserId",
            new { TeamId = teamId, UserId = userId },
            session.Transaction)) > 0;

    public async Task<List<int>> FindOccupiedSlots(long teamId)
    {
        var slots = await Connection().QueryAsync<int>(new CommandDefinition(
            "SELECT slot FROM tools_temtem.team_member WHERE team_id = @TeamId",
            new { TeamId = teamId },
            session.Transaction));

        return slots.ToList();
    }

    public Task<long> AddMember(long teamId, int temtemId, int slot) =>
        Connection().ExecuteScalarAsync<long>(new CommandDefinition(
            """
            INSERT INTO tools_temtem.team_member (team_id, temtem_id, slot)
            VALUES (@TeamId, @TemtemId, @Slot)
            RETURNING id
            """,
            new { TeamId = teamId, TemtemId = temtemId, Slot = slot },
            session.Transaction));

    public async Task ReorderMembers(long teamId, IReadOnlyList<long> memberIds)
    {
        await Connection().ExecuteAsync(new CommandDefinition(
            """
            SET CONSTRAINTS tools_temtem.uq_temtem_team_member_slot DEFERRED;

            UPDATE tools_temtem.team_member AS member
            SET slot = ordered.slot::int
            FROM unnest(@MemberIds::bigint[]) WITH ORDINALITY AS ordered(member_id, slot)
            WHERE member.team_id = @TeamId AND member.id = ordered.member_id
            """,
            new { TeamId = teamId, MemberIds = memberIds.ToArray() },
            session.Transaction));
    }

    public Task<int?> FindMemberTemtemId(long teamId, long memberId) =>
        Connection().ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT temtem_id FROM tools_temtem.team_member WHERE id = @MemberId AND team_id = @TeamId",
            new { TeamId = teamId, MemberId = memberId },
            session.Transaction));

    public async Task<bool> DeleteMember(long teamId, long memberId) =>
        await Connection().ExecuteAsync(new CommandDefinition(
            "DELETE FROM tools_temtem.team_member WHERE id = @MemberId AND team_id = @TeamId",
            new { TeamId = teamId, MemberId = memberId },
            session.Transaction)) > 0;

    // Remplacement total : on efface puis on réinsère. Un différentiel coûterait deux lectures
    // pour au mieux économiser quatre lignes.
    public async Task ReplaceMemberTechniques(long memberId, IReadOnlyCollection<int> techniqueIds)
    {
        await Connection().ExecuteAsync(new CommandDefinition(
            "DELETE FROM tools_temtem.team_member_technique WHERE team_member_id = @MemberId",
            new { MemberId = memberId },
            session.Transaction));

        if (techniqueIds.Count == 0)
        {
            return;
        }

        await Connection().ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO tools_temtem.team_member_technique (team_member_id, technique_id)
            SELECT @MemberId, unnest(@TechniqueIds::int[])
            """,
            new { MemberId = memberId, TechniqueIds = techniqueIds.ToArray() },
            session.Transaction));
    }

    public Task TouchUpdatedAt(long teamId) =>
        Connection().ExecuteAsync(new CommandDefinition(
            "UPDATE tools_temtem.team SET updated_at = now() WHERE id = @TeamId",
            new { TeamId = teamId },
            session.Transaction));

    // Le filtre porte sur les équipes ; membres et techniques suivent par les identifiants
    // trouvés, ce qui évite de répéter la condition d'appartenance dans trois requêtes.
    private async Task<List<TemtemTeamView>> Load(string where, object parameters)
    {
        var teamsSql = $"""
            SELECT tm.id AS Id, tm.name AS Name, tm.created_at AS CreatedAt, tm.updated_at AS UpdatedAt
            FROM tools_temtem.team tm
            {where}
            ORDER BY tm.name
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var teamRows = (await connection.QueryAsync<TeamRow>(
            new CommandDefinition(teamsSql, parameters))).ToList();

        if (teamRows.Count == 0)
        {
            return [];
        }

        var teamIds = teamRows.Select(team => team.Id).ToArray();

        var membersSql = $"""
            SELECT mb.id AS MemberId, mb.team_id AS TeamId, mb.slot AS Slot,
                   {TemtemCreatureSql.Columns}
            FROM tools_temtem.team_member mb
            JOIN tools_temtem.temtem t ON t.id = mb.temtem_id
            {TemtemCreatureSql.Joins}
            WHERE mb.team_id = ANY(@TeamIds)
            ORDER BY mb.team_id, mb.slot
            """;

        var memberRows = (await connection.QueryAsync<MemberRow>(
            new CommandDefinition(membersSql, new { TeamIds = teamIds }))).ToList();

        var techniquesByMember = memberRows.Count == 0
            ? []
            : await LoadTechniques(connection, memberRows.Select(member => member.MemberId).ToArray());

        var membersByTeam = memberRows
            .GroupBy(member => member.TeamId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(member => new TemtemTeamMemberView(
                        member.MemberId,
                        member.Slot,
                        TemtemCreatureSql.ToView(member),
                        techniquesByMember.TryGetValue(member.MemberId, out var techniques)
                            ? techniques
                            : []))
                    .ToList());

        return teamRows
            .Select(team => new TemtemTeamView(
                team.Id,
                team.Name,
                membersByTeam.TryGetValue(team.Id, out var members) ? members : [],
                team.CreatedAt,
                team.UpdatedAt))
            .ToList();
    }

    private static async Task<Dictionary<long, List<TemtemTechniqueView>>> LoadTechniques(NpgsqlConnection connection, long[] memberIds)
    {
        var sql = $"""
            SELECT tmt.team_member_id AS MemberId, {TemtemTechniqueSql.Columns}
            FROM tools_temtem.team_member_technique tmt
            JOIN tools_temtem.technique tec ON tec.id = tmt.technique_id
            {TemtemTechniqueSql.Joins}
            WHERE tmt.team_member_id = ANY(@MemberIds)
            ORDER BY tec.name
            """;

        var rows = await connection.QueryAsync<MemberTechniqueRow>(
            new CommandDefinition(sql, new { MemberIds = memberIds }));

        return rows
            .GroupBy(row => row.MemberId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(TemtemTechniqueSql.ToView).ToList());
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction ouverte : cette écriture doit être encadrée.");

    private sealed record TeamRow(long Id, string Name, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed class MemberRow : TemtemCreatureSql.Row
    {
        public long MemberId { get; set; }
        public long TeamId { get; set; }
        public int Slot { get; set; }
    }

    private sealed class MemberTechniqueRow : TemtemTechniqueSql.Row
    {
        public long MemberId { get; set; }
    }
}
