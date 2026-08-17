namespace Tools.Api.Modules.Admin.Application.Dto;

// Nombre d'utilisateurs ayant accès à un module donné.
//
// Le module est identifié par son code, pas par son identifiant — c'est ce que renvoie l'API
// Java, et le contrat est reproduit tel quel. Le type TypeScript du frontend déclare pourtant
// un champ `moduleId` : il est donc toujours indéfini côté Java comme ici. Corriger l'un sans
// l'autre pendant la bascule rendrait un éventuel bug inattribuable.
public sealed record ModuleUserCountDto(
    string ModuleCode,
    string ModuleName,
    long UserCount
);
