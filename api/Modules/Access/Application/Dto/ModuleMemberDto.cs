namespace Tools.Api.Modules.Access.Application.Dto;

// Un utilisateur ayant accès à un module, avec le rôle qu'il y détient.
//
// Le rôle est unique ici alors que la table en autorise plusieurs : c'est le plus permissif
// qui est retenu (voir PostgresModuleMembershipRepository). Le frontend n'en affiche qu'un et
// n'en attribue qu'un.
public sealed record ModuleMemberDto(
    long UserId,
    string Email,
    string Name,
    long RoleId,
    string RoleCode
);
