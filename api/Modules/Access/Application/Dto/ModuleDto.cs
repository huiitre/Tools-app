namespace Tools.ApiCore.Modules.Access.Application.Dto;

// Reflète tools_core.module : un module fonctionnel de l'application (Dofus, Palworld…),
// tel que l'administration le gère. À ne pas confondre avec un module de code du Core.
public sealed record ModuleDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    bool Active,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
