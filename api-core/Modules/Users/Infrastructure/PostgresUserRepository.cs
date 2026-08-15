using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Users.Application;
using Tools.ApiCore.Modules.Users.Application.Dto;

namespace Tools.ApiCore.Modules.Users.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IUserRepository.
public sealed class PostgresUserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    public async Task<UserProfileDto?> FindProfileAsync(long userId)
    {
        // Trois lectures envoyées en un seul aller-retour : le profil comporte deux
        // collections indépendantes (rôles globaux, modules), et les réunir en une seule
        // requête produirait un produit cartésien à dédoublonner ensuite.
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
            WHERE ur.user_id = @UserId
            ORDER BY r.id;

            SELECT m.id AS ModuleId, m.code AS ModuleCode, m.name AS ModuleName,
                   m.description AS ModuleDescription, m.is_active AS ModuleActive,
                   r.id AS RoleId, r.code AS RoleCode, r.name AS RoleName,
                   r.description AS RoleDescription, r.is_active AS RoleActive
            FROM tools_core.user_module_role umr
            INNER JOIN tools_core.module m ON m.id = umr.module_id
            INNER JOIN tools_core.role r ON r.id = umr.role_id
            WHERE umr.user_id = @UserId
            ORDER BY m.id, r.id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId }));

        var user = await results.ReadSingleOrDefaultAsync<UserRow>();
        if (user is null)
        {
            return null;
        }

        var roles = (await results.ReadAsync<RoleDto>()).ToList();
        var moduleRows = await results.ReadAsync<ModuleRoleRow>();

        // La clé primaire de user_module_role est (user_id, module_id, role_id) : un même
        // module peut donc apparaître sur plusieurs lignes, une par rôle détenu.
        var modules = moduleRows
            .GroupBy(row => new { row.ModuleId, row.ModuleCode, row.ModuleName, row.ModuleDescription, row.ModuleActive })
            .Select(group => new UserModuleDto(
                group.Key.ModuleId,
                group.Key.ModuleCode,
                group.Key.ModuleName,
                group.Key.ModuleDescription,
                group.Key.ModuleActive,
                group
                    .Select(row => new RoleDto(row.RoleId, row.RoleCode, row.RoleName, row.RoleDescription, row.RoleActive))
                    .ToList()))
            .ToList();

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.Name,
            user.UserType,
            user.Active,
            user.AvatarUrl,
            roles,
            modules);
    }

    private sealed record UserRow(
        long Id,
        string Email,
        string Name,
        string UserType,
        bool Active,
        string? AvatarUrl);

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
