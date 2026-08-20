namespace Tools.Api.Modules.Users.Application.Dto;

// Reflète tools_core.module, complété par le rôle que l'utilisateur détient sur ce module.
// La clé primaire de user_module_role est (user_id, module_id) : un utilisateur n'y détient
// qu'un rôle, d'où une valeur unique et non une liste.
public sealed record UserModuleDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    bool Active,
    RoleDto Role
);
