using Tools.ApiCore.Modules.Security.Application.Dto;
using Tools.ApiCore.Modules.Security.Application.Ports;

namespace Tools.ApiCore.IntegrationTests.Fakes;

// Catalogue des rôles en mémoire. Les identifiants reproduisent ceux du référentiel réel,
// où ADMIN (4) précède TECH (5) — l'inverse de la hiérarchie.
public sealed class InMemoryRoleRepository : IRoleRepository
{
    private static readonly RoleDto[] Roles =
    [
        new(1, "READ_ONLY", "Lecture seule", "Accès en lecture seule", true),
        new(2, "USER", "Utilisateur", "Usage fonctionnel standard", true),
        new(3, "MODERATOR", "Modérateur", "Actions métier intermédiaires", true),
        new(4, "ADMIN", "Administrateur", "Administration fonctionnelle", true),
        new(5, "TECH", "Technique", "Accès technique", true),
        new(6, "OWNER", "Propriétaire", "Le GOD", true)
    ];

    public Task<IReadOnlyList<RoleDto>> FindAllAsync() =>
        Task.FromResult<IReadOnlyList<RoleDto>>(Roles);

    public Task<bool> ExistsAsync(long roleId) =>
        Task.FromResult(Roles.Any(role => role.Id == roleId));

    public Task<long?> FindIdByCodeAsync(string code) =>
        Task.FromResult(Roles.FirstOrDefault(role => role.Code == code)?.Id);
}
