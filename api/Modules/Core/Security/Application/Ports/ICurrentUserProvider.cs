using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Security.Application.Ports;

// Port d'identification de l'appelant : l'Application ignore d'où vient l'identité
// (en-tête HTTP, token, autre) et ne connaît que son identifiant et ses rôles.
public interface ICurrentUserProvider
{
    // Appelant authentifié, ou null si l'appel est anonyme ou effectué hors d'une
    // requête HTTP (tâche de fond, par exemple).
    CurrentUser? Current { get; }
}

// Les rôles proviennent de l'access token et font autorité pendant sa durée de vie :
// un droit retiré s'applique au renouvellement du token, pas immédiatement.
//
// Un utilisateur porte **au plus un rôle global et au plus un rôle par module** : les clés
// primaires de `tools_core.user_role` et `tools_core.user_module_role` l'imposent. Il n'y a
// donc aucun arbitrage à faire ici, seulement une valeur à lire — un rôle est présent ou il
// ne l'est pas.
//
// Deux natures de droits cohabitent et ne se mélangent pas : `Role` vaut pour le site,
// `ModuleRoles` vaut à l'intérieur d'un module. Un utilisateur peut être administrateur du
// site et n'avoir aucun droit sur un module donné — c'est une situation normale, pas une
// incohérence à rattraper.
public sealed record CurrentUser(
    long UserId,
    RoleCode? Role,
    IReadOnlyDictionary<ModuleCode, RoleCode> ModuleRoles)
{
    // Rôle détenu à l'intérieur d'un module, ou null si l'utilisateur n'y a aucun accès.
    // Le rôle global n'y participe pas : un administrateur du site déclaré READ_ONLY sur un
    // module y est READ_ONLY, et un administrateur du site absent d'un module n'y entre pas.
    // Le rôle sur un module est un droit à part entière, pas un plancher que le rôle global
    // relèverait.
    public RoleCode? RoleIn(ModuleCode module) =>
        ModuleRoles.TryGetValue(module, out var role) ? role : null;
}
