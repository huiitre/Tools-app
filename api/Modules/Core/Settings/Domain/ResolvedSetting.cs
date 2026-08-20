using System.Text.Json.Nodes;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Un paramètre tel qu'il s'applique à quelqu'un : sa valeur effective, d'où elle vient, et ce
// que cette personne a le droit d'en faire.
//
// `Source` et `CanSetOwn` ne sont pas des commodités d'affichage : sans eux, le frontend
// devrait rejouer la résolution pour savoir s'il propose « Réinitialiser » ou s'il grise le
// champ — donc réimplémenter la règle, donc diverger.
public sealed record ResolvedSetting(
    SettingDefinition Definition,
    JsonNode Value,

    // Portée d'où vient la valeur retenue, ou null si c'est le défaut du catalogue.
    SettingScope? Source,

    // La valeur est imposée par un verrou posé à une portée moins prioritaire.
    bool IsLocked,

    // L'appelant peut poser sa propre valeur : le paramètre accepte la portée User, son rôle
    // suffit, et aucun verrou ne l'en empêche.
    bool CanSetOwn)
{
    // Une valeur propre existe et peut être retirée pour revenir à l'héritage.
    public bool CanReset => Source == SettingScope.User && CanSetOwn;
}
