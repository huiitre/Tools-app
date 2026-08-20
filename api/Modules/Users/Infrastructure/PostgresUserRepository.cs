using Dapper;
using Npgsql;
using Tools.Api.Modules.Common.Infrastructure;
using Tools.Api.Modules.Users.Application;
using Tools.Api.Modules.Users.Application.Dto;

namespace Tools.Api.Modules.Users.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IUserRepository.
//
// Les lectures ouvrent leur propre connexion ; l'écriture du rôle global passe par la
// transaction du use case, sans quoi la suppression et l'insertion ne seraient pas atomiques.
public sealed class PostgresUserRepository(
    NpgsqlDataSource dataSource,
    PostgresSession session) : IUserRepository
{
    public async Task<UserProfileDto?> FindProfileAsync(long userId)
    {
        // Trois lectures envoyées en un seul aller-retour : le rôle global et les modules
        // sont indépendants, et les réunir en une seule requête multiplierait les lignes de
        // module par celle du rôle.
        const string sql = """
            SELECT u.id AS Id, u.email AS Email, u.name AS Name,
                   u.user_type AS UserType, u.is_active AS Active,
                   provider.provider_avatar_url AS AvatarUrl
            FROM tools_core.users u
            LEFT JOIN tools_core.user_auth_provider provider
                ON provider.user_id = u.id AND provider.provider = u.avatar_source
            WHERE u.id = @UserId;

            SELECT r.id AS Id, r.code AS Code, r.name AS Name,
                   r.description AS Description, r.is_active AS Active
            FROM tools_core.user_role ur
            INNER JOIN tools_core.role r ON r.id = ur.role_id
            WHERE ur.user_id = @UserId;

            SELECT m.id AS ModuleId, m.code AS ModuleCode, m.name AS ModuleName,
                   m.description AS ModuleDescription, m.is_active AS ModuleActive,
                   r.id AS RoleId, r.code AS RoleCode, r.name AS RoleName,
                   r.description AS RoleDescription, r.is_active AS RoleActive
            FROM tools_core.user_module_role umr
            INNER JOIN tools_core.module m ON m.id = umr.module_id
            INNER JOIN tools_core.role r ON r.id = umr.role_id
            WHERE umr.user_id = @UserId
            ORDER BY m.id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId }));

        var user = await results.ReadSingleOrDefaultAsync<UserRow>();
        if (user is null)
        {
            return null;
        }

        // Au plus une ligne de rôle global, au plus une ligne par module : les deux clés
        // primaires l'imposent, il n'y a rien à regrouper.
        var role = await results.ReadSingleOrDefaultAsync<RoleDto>();
        var modules = (await results.ReadAsync<ModuleRoleRow>())
            .Select(row => new UserModuleDto(
                row.ModuleId,
                row.ModuleCode,
                row.ModuleName,
                row.ModuleDescription,
                row.ModuleActive,
                new RoleDto(row.RoleId, row.RoleCode, row.RoleName, row.RoleDescription, row.RoleActive)))
            .ToList();

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.Name,
            user.UserType,
            user.Active,
            user.AvatarUrl,
            role,
            modules);
    }

    public async Task<IReadOnlyList<UserAdminDto>> FindAllForAdminAsync()
    {
        // Une ligne par utilisateur : la jointure sur user_role ne peut pas en produire deux,
        // sa clé primaire étant (user_id). L'avatar vient du provider Google uniquement, comme
        // dans l'API Java — un compte créé par mot de passe n'en a pas.
        const string sql = """
            SELECT u.id AS Id, u.email AS Email, u.name AS Name,
                   u.is_active AS Active, u.created_at AS CreatedAt,
                   provider.provider_avatar_url AS AvatarUrl,
                   ur.role_id AS RoleId
            FROM tools_core.users u
            LEFT JOIN tools_core.user_role ur ON ur.user_id = u.id
            LEFT JOIN tools_core.user_auth_provider provider
                ON provider.user_id = u.id AND provider.provider = 'GOOGLE'
            ORDER BY u.created_at DESC, u.id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<UserAdminRow>(new CommandDefinition(sql));

        return rows
            .Select(row => new UserAdminDto(
                row.Id,
                row.Email,
                row.Name,
                row.Active,
                row.CreatedAt,
                row.AvatarUrl,
                row.RoleId))
            .ToList();
    }

    public async Task<bool> ExistsAsync(long userId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.users WHERE id = @UserId)";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId }));
    }

    public async Task ReplaceGlobalRoleAsync(long userId, long roleId)
    {
        // (user_id) étant la clé primaire, l'attribution est un simple upsert : la ligne
        // existante est mise à jour, sinon elle est créée. Le couple DELETE + INSERT que cette
        // méthode faisait avant n'avait de raison d'être que pour effacer un éventuel cumul.
        const string sql = """
            INSERT INTO tools_core.user_role (user_id, role_id)
            VALUES (@UserId, @RoleId)
            ON CONFLICT (user_id) DO UPDATE SET role_id = EXCLUDED.role_id
            """;

        await Connection().ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, RoleId = roleId }, session.Transaction));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");

    private sealed record UserRow(
        long Id,
        string Email,
        string Name,
        string UserType,
        bool Active,
        string? AvatarUrl);

    private sealed record UserAdminRow(
        long Id,
        string Email,
        string Name,
        bool Active,
        DateTime? CreatedAt,
        string? AvatarUrl,
        long? RoleId);

    private sealed record ModuleRoleRow(
        long ModuleId,
        string ModuleCode,
        string ModuleName,
        string? ModuleDescription,
        bool ModuleActive,
        long RoleId,
        string RoleCode,
        string RoleName,
        string? RoleDescription,
        bool RoleActive);
}
