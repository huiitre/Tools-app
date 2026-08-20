namespace Tools.Api.Modules.Core.Access.Application.Dto;

// Un utilisateur ayant accès à un module, avec le rôle qu'il y détient. Un seul rôle par
// membre : (user_id, module_id) est la clé primaire de tools_core.user_module_role.
public sealed record ModuleMemberDto(
    long UserId,
    string Email,
    string Name,
    long RoleId,
    string RoleCode
);
