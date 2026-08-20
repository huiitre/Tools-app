using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.Access.Application.Dto;
using Tools.Api.Modules.Core.Access.Application.Ports;
using Tools.Api.Modules.Core.Common.Infrastructure;

namespace Tools.Api.Modules.Core.Access.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IModuleMembershipRepository.
//
// La clé primaire de user_module_role est (user_id, module_id) depuis V2.4.0 : un utilisateur
// détient au plus un rôle par module. Ni la lecture ni l'écriture n'ont donc de cumul à
// arbitrer.
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

        return rows.AsList();
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

    // (user_id, module_id) étant la clé primaire, accorder un accès et changer un rôle sont la
    // même opération : un upsert. Le couple DELETE + INSERT d'avant ne servait qu'à effacer un
    // éventuel cumul.
    private async Task ReplaceRoleAsync(long moduleId, long userId, long roleId)
    {
        const string sql = """
            INSERT INTO tools_core.user_module_role (user_id, module_id, role_id)
            VALUES (@UserId, @ModuleId, @RoleId)
            ON CONFLICT (user_id, module_id) DO UPDATE SET role_id = EXCLUDED.role_id
            """;

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { ModuleId = moduleId, UserId = userId, RoleId = roleId },
            session.Transaction));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
