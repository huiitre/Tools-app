using Dapper;
using Npgsql;
using Tools.Api.Modules.Access.Application.Dto;
using Tools.Api.Modules.Access.Application.Ports;
using Tools.Api.Modules.Common.Infrastructure;
using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Access.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IModuleMembershipRepository.
//
// La clé primaire de user_module_role est (user_id, module_id, role_id) : la table autorise
// donc plusieurs rôles pour une même paire, alors que le frontend n'en attribue qu'un et que
// tout le code suppose l'unicité. En lecture, c'est le rôle le plus permissif qui est retenu ;
// en écriture, les lignes existantes sont supprimées avant l'insertion, si bien que le Core ne
// crée jamais de doublon. La contrainte sera rendue explicite par une migration ultérieure.
public sealed class PostgresModuleMembershipRepository(
    NpgsqlDataSource dataSource,
    PostgresSession session) : IModuleMembershipRepository
{
    public async Task<IReadOnlyList<ModuleMemberDto>> FindMembersAsync(long moduleId)
    {
        const string sql = """
            SELECT u.id AS UserId, u.email AS Email, u.name AS Name,
                   r.id AS RoleId, r.code AS RoleCode
            FROM tools_core.user_module_role umr
            INNER JOIN tools_core.users u ON u.id = umr.user_id
            INNER JOIN tools_core.role r ON r.id = umr.role_id
            WHERE umr.module_id = @ModuleId
            ORDER BY u.name
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<ModuleMemberDto>(
            new CommandDefinition(sql, new { ModuleId = moduleId }));

        // Le tri se fait sur RoleCode et jamais sur role_id : les identifiants suivent l'ordre
        // d'insertion du référentiel, où ADMIN précède TECH, alors que la hiérarchie place
        // TECH en dessous d'ADMIN. Trier par identifiant inverserait ces deux rôles.
        return rows
            .GroupBy(member => new { member.UserId, member.Email, member.Name })
            .Select(group => group
                .OrderByDescending(member => RoleCodes.Parse(member.RoleCode) ?? 0)
                .First())
            .OrderBy(member => member.Name)
            .ToList();
    }

    public async Task<bool> HasAccessAsync(long moduleId, long userId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tools_core.user_module_role
                WHERE module_id = @ModuleId AND user_id = @UserId
            )
            """;

        // Lue hors transaction comme dans la transaction : les use cases d'écriture appellent
        // cette vérification avant d'ouvrir la leur.
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { ModuleId = moduleId, UserId = userId }));
    }

    public Task GrantAsync(long moduleId, long userId, long roleId) =>
        ReplaceRoleAsync(moduleId, userId, roleId);

    public Task ChangeRoleAsync(long moduleId, long userId, long roleId) =>
        ReplaceRoleAsync(moduleId, userId, roleId);

    public async Task RevokeAsync(long moduleId, long userId)
    {
        const string sql = """
            DELETE FROM tools_core.user_module_role
            WHERE module_id = @ModuleId AND user_id = @UserId
            """;

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { ModuleId = moduleId, UserId = userId },
            session.Transaction));
    }

    // Un seul rôle par paire (utilisateur, module) : les lignes existantes disparaissent avant
    // l'insertion. Un UPDATE ne conviendrait pas — s'il en existait deux, il les amènerait
    // toutes deux sur le même role_id et violerait la clé primaire.
    private async Task ReplaceRoleAsync(long moduleId, long userId, long roleId)
    {
        const string deleteSql = """
            DELETE FROM tools_core.user_module_role
            WHERE module_id = @ModuleId AND user_id = @UserId
            """;

        const string insertSql = """
            INSERT INTO tools_core.user_module_role (user_id, module_id, role_id)
            VALUES (@UserId, @ModuleId, @RoleId)
            """;

        var connection = Connection();
        await connection.ExecuteAsync(new CommandDefinition(
            deleteSql,
            new { ModuleId = moduleId, UserId = userId },
            session.Transaction));
        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            new { ModuleId = moduleId, UserId = userId, RoleId = roleId },
            session.Transaction));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
