namespace Tools.Api.Modules.Users.Application.Dto;

// Reflète tools_core.role. `Description` est nullable en base ; `Active` correspond à
// `is_active`, que le frontend filtre avant de retenir un rôle.
public sealed record RoleDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    bool Active
);
