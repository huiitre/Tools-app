using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.Access.Application.Dto;
using Tools.Api.Modules.Core.Access.Application.Ports;

namespace Tools.Api.Modules.Core.Access.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IModuleRepository.
//
// La création et la mise à jour d'un module touchent une seule ligne : elles n'ont pas besoin
// d'une transaction et ouvrent leur propre connexion, contrairement aux écritures
// d'appartenance qui suppriment avant d'insérer.
public sealed class PostgresModuleRepository(NpgsqlDataSource dataSource) : IModuleRepository
{
    public async Task<IReadOnlyList<ModuleDto>> FindAllAsync()
    {
        const string sql = """
            SELECT id AS Id, code AS Code, name AS Name, description AS Description,
                   is_active AS Active, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tools_core.module
            ORDER BY id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var modules = await connection.QueryAsync<ModuleDto>(new CommandDefinition(sql));
        return modules.ToList();
    }

    public async Task<bool> ExistsAsync(long moduleId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.module WHERE id = @ModuleId)";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { ModuleId = moduleId }));
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludedModuleId = null)
    {
        // @ExcludedModuleId vaut NULL à la création : la comparaison est alors toujours fausse
        // et aucune ligne n'est exclue.
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tools_core.module
                WHERE code = @Code
                  AND (@ExcludedModuleId IS NULL OR id <> @ExcludedModuleId)
            )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Code = code, ExcludedModuleId = excludedModuleId }));
    }

    public async Task<long> CreateAsync(string code, string name, string? description)
    {
        // is_active reste false : un module s'active dans un second temps.
        const string sql = """
            INSERT INTO tools_core.module (code, name, description, is_active)
            VALUES (@Code, @Name, @Description, false)
            RETURNING id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<long>(
            new CommandDefinition(sql, new { Code = code, Name = name, Description = description }));
    }

    public async Task UpdateAsync(long moduleId, string code, string name, string? description, bool active)
    {
        const string sql = """
            UPDATE tools_core.module
            SET code = @Code, name = @Name, description = @Description,
                is_active = @Active, updated_at = now()
            WHERE id = @ModuleId
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ModuleId = moduleId, Code = code, Name = name, Description = description, Active = active }));
    }
}
