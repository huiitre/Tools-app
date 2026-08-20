using System.Text.Json.Nodes;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Une ligne de `tools_core.setting_value`, telle qu'elle a été lue, sans interprétation.
//
// Ce type ne sait pas s'il fait autorité : c'est un candidat que `SettingResolution` retiendra
// ou écartera. Le stocker tel quel permet de rendre l'origine d'une valeur effective au
// frontend — « hérité du global » ne se déduit pas d'une valeur seule.
public sealed record SettingValue(
    string      Code,
    SettingScope Scope,

    // Renseigné si et seulement si Scope vaut Role — la contrainte CHECK de la table l'impose.
    RoleCode?   Role,

    // Renseigné si et seulement si Scope vaut User.
    long?       UserId,

    JsonNode    Value,

    // La valeur s'impose : aucune portée plus prioritaire ne peut la remplacer.
    bool        IsLocked);
