using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Security.Application.Dto;
using Tools.ApiCore.Modules.Security.Application.Ports;

namespace Tools.ApiCore.Modules.Security.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IRoleRepository.
//
// Lectures seules : elles n'ont pas à participer à la transaction d'un use case d'écriture,
// le catalogue des rôles n'étant jamais modifié par l'application.
public sealed class PostgresRoleRepository(NpgsqlDataSource dataSource) : IRoleRepository
{
    public async Task<IReadOnlyList<RoleDto>> FindAllAsync()
    {
        // L'API Java ne trie pas ; l'ordre par identifiant reproduit la hiérarchie
        // d'insertion du référentiel et rend la réponse stable d'un appel à l'autre.
        const string sql = """
            SELECT id AS Id, code AS Code, name AS Name,
                   description AS Description, is_active AS Active
            FROM tools_core.role
            ORDER BY id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var roles = await connection.QueryAsync<RoleDto>(new CommandDefinition(sql));
        return roles.ToList();
    }

    public async Task<bool> ExistsAsync(long roleId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.role WHERE id = @RoleId)";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RoleId = roleId }));
    }

    public async Task<long?> FindIdByCodeAsync(string code)
    {
        const string sql = "SELECT id FROM tools_core.role WHERE code = @Code";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<long?>(
            new CommandDefinition(sql, new { Code = code }));
    }
}
