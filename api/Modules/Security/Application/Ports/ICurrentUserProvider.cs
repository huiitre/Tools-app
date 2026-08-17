using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Security.Application.Ports;

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
// Deux natures de droits cohabitent et ne se mélangent pas : `Roles` vaut pour le site,
// `ModuleRoles` vaut à l'intérieur d'un module. Un utilisateur peut être administrateur du
// site et n'avoir aucun droit sur un module donné — c'est une situation normale, pas une
// incohérence à rattraper.
public sealed record CurrentUser(
    long UserId,
    IReadOnlyCollection<RoleCode> Roles,
    IReadOnlyDictionary<ModuleCode, IReadOnlyCollection<RoleCode>> ModuleRoles)
{
    // Rôle effectif : le plus permissif de ceux portés par le token.
    public RoleCode? HighestRole => Roles.Count == 0 ? null : Roles.Max();

    // Rôle effectif à l'intérieur d'un module, ou null si l'utilisateur n'y a aucun accès.
    // Les rôles globaux n'y participent pas : un administrateur du site déclaré READ_ONLY sur
    // un module y est READ_ONLY, et un administrateur du site absent d'un module n'y entre pas.
    // Le rôle sur un module est un droit à part entière, pas un plancher que le rôle global
    // relèverait.
    public RoleCode? HighestRoleIn(ModuleCode module) =>
        ModuleRoles.TryGetValue(module, out var roles) && roles.Count > 0 ? roles.Max() : null;
}
