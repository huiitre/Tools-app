using Dapper;
using Npgsql;
using Tools.Api.Modules.Auth.Application.Ports;
using Tools.Api.Modules.Auth.Domain;

namespace Tools.Api.Modules.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du port IAuthRepository.
public sealed class PostgresAuthRepository(NpgsqlDataSource dataSource) : IAuthRepository
{
    public async Task<(AuthUser User, string PasswordHash)?> FindPasswordLoginAsync(string email)
    {
        // Seuls les comptes ayant le provider PASSWORD et des credentials peuvent se connecter ici.
        const string sql = """
            SELECT u.id AS Id, u.email AS Email, u.is_active AS IsActive, u.user_type AS UserType,
                   credentials.password_hash AS PasswordHash
            FROM tools_core.users u
            INNER JOIN tools_core.user_auth_provider provider
                ON provider.user_id = u.id AND provider.provider = 'PASSWORD'
            INNER JOIN tools_core.user_credentials credentials ON credentials.user_id = u.id
            WHERE u.email = @Email
            """;

        // La connexion est rendue au pool automatiquement à la fin de la méthode.
        await using var connection = await dataSource.OpenConnectionAsync();
        var row = await connection.QuerySingleOrDefaultAsync<LoginRow>(
            new CommandDefinition(sql, new { Email = email }));
        // La ligne technique Dapper est convertie en objet métier minimal.
        return row is null ? null : (new AuthUser(row.Id, row.Email, row.IsActive, row.UserType), row.PasswordHash);
    }

    public async Task<AuthUser?> FindByIdAsync(long userId)
    {
        // Cette lecture sert au refresh pour vérifier l'état actuel du compte.
        const string sql = "SELECT id AS Id, email AS Email, is_active AS IsActive, user_type AS UserType FROM tools_core.users WHERE id = @UserId";
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<AuthUser>(new CommandDefinition(sql, new { UserId = userId }));
    }

    public async Task<AuthUser?> FindByEmailAsync(string email)
    {
        const string sql = "SELECT id AS Id, email AS Email, is_active AS IsActive, user_type AS UserType FROM tools_core.users WHERE email = @Email";
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<AuthUser>(new CommandDefinition(sql, new { Email = email }));
    }

    public async Task<string?> FindGlobalRoleAsync(long userId)
    {
        // Au plus une ligne : `user_role` a pour clé primaire (user_id). Un rôle désactivé au
        // référentiel ne va pas dans le token, et l'utilisateur se retrouve alors sans rôle.
        const string sql = "SELECT r.code FROM tools_core.user_role ur INNER JOIN tools_core.role r ON r.id = ur.role_id WHERE ur.user_id = @UserId AND r.is_active = true";
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(sql, new { UserId = userId }));
    }

    public async Task<IReadOnlyDictionary<string, string>> FindModuleRolesAsync(long userId)
    {
        // Chaque entrée associe un code module au rôle actif qu'y détient cet utilisateur.
        // Une seule ligne par module : (user_id, module_id) est la clé primaire de
        // `user_module_role` depuis V2.4.0.
        const string sql = """
            SELECT m.code AS ModuleCode, r.code AS RoleCode
            FROM tools_core.user_module_role umr
            INNER JOIN tools_core.module m ON m.id = umr.module_id
            INNER JOIN tools_core.role r ON r.id = umr.role_id
            WHERE umr.user_id = @UserId AND m.is_active = true AND r.is_active = true
            """;
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<ModuleRoleRow>(new CommandDefinition(sql, new { UserId = userId }));
        return rows.ToDictionary(
            row => row.ModuleCode,
            row => row.RoleCode,
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed record LoginRow(long Id, string Email, bool IsActive, string UserType, string PasswordHash);
    private sealed record ModuleRoleRow(string ModuleCode, string RoleCode);
}
