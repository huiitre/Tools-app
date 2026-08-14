using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Auth.Domain;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du port IAuthRepository.
public sealed class PostgresAuthRepository(NpgsqlDataSource dataSource) : IAuthRepository
{
    public async Task<(AuthUser User, string PasswordHash)?> FindPasswordLoginAsync(string email, CancellationToken cancellationToken)
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
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<LoginRow>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        // La ligne technique Dapper est convertie en objet métier minimal.
        return row is null ? null : (new AuthUser(row.Id, row.Email, row.IsActive, row.UserType), row.PasswordHash);
    }

    public async Task<AuthUser?> FindByIdAsync(long userId, CancellationToken cancellationToken)
    {
        // Cette lecture sert au refresh pour vérifier l'état actuel du compte.
        const string sql = "SELECT id AS Id, email AS Email, is_active AS IsActive, user_type AS UserType FROM tools_core.users WHERE id = @UserId";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthUser>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<AuthUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id AS Id, email AS Email, is_active AS IsActive, user_type AS UserType FROM tools_core.users WHERE email = @Email";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuthUser>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<string>> FindGlobalRolesAsync(long userId, CancellationToken cancellationToken)
    {
        // Seuls les rôles encore actifs sont mis dans le token.
        const string sql = "SELECT r.code FROM tools_core.user_role ur INNER JOIN tools_core.role r ON r.id = ur.role_id WHERE ur.user_id = @UserId AND r.is_active = true";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return (await connection.QueryAsync<string>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken))).AsList();
    }

    public async Task<IReadOnlyDictionary<string, string>> FindModuleRolesAsync(long userId, CancellationToken cancellationToken)
    {
        // Chaque entrée associe un code module à son rôle actif pour cet utilisateur.
        const string sql = """
            SELECT m.code AS ModuleCode, r.code AS RoleCode
            FROM tools_core.user_module_role umr
            INNER JOIN tools_core.module m ON m.id = umr.module_id
            INNER JOIN tools_core.role r ON r.id = umr.role_id
            WHERE umr.user_id = @UserId AND m.is_active = true AND r.is_active = true
            """;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ModuleRoleRow>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return rows.ToDictionary(row => row.ModuleCode, row => row.RoleCode, StringComparer.OrdinalIgnoreCase);
    }

    private sealed record LoginRow(long Id, string Email, bool IsActive, string UserType, string PasswordHash);
    private sealed record ModuleRoleRow(string ModuleCode, string RoleCode);
}
