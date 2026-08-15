namespace Tools.ApiCore.Modules.Users.Application.Dto;

// Reflète tools_core.module, complété par les rôles que l'utilisateur détient sur ce module.
// La clé primaire de user_module_role est (user_id, module_id, role_id) : plusieurs rôles par
// module sont donc possibles, d'où une liste et non une valeur unique.
public sealed record UserModuleDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    bool Active,
    IReadOnlyList<RoleDto> Roles
);
