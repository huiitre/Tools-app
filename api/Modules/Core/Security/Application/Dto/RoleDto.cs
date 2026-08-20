namespace Tools.Api.Modules.Core.Security.Application.Dto;

// Une ligne du catalogue tools_core.role.
//
// Distinct du RoleDto de Users, qui décrit un rôle *détenu par quelqu'un* à l'intérieur d'un
// profil. Ici c'est le référentiel lui-même : la liste des rôles attribuables, telle que
// l'administration la présente. Les deux ont la même forme aujourd'hui et n'ont aucune raison
// d'évoluer ensemble.
public sealed record RoleDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    bool Active
);
