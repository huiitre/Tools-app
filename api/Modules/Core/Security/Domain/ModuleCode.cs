namespace Tools.Api.Modules.Core.Security.Domain;

// Modules sur lesquels une autorisation peut porter, tels que `tools_core.module.code` les
// nomme. L'énumération n'existe que pour qu'un use case désigne son module autrement que par
// une chaîne libre : une faute de frappe dans une chaîne ne se voit qu'à l'exécution, et un
// module que personne ne reconnaît est un contrôle qui ne s'applique à personne.
//
// Ce type vit dans Security et non dans Access : Access administre les modules (leur création,
// leurs membres), Security décide des droits. C'est ici qu'il est consommé, et Access dépend
// déjà de Security.
public enum ModuleCode
{
    Health,
    Todolist,
    Dofus,
    Riot,
    EliteDangerous,
    Palworld,
    Temtem,
    Codename
}

public static class ModuleCodes
{
    // Codes tels qu'ils sont stockés en base, insensibles à la casse. La convention vient de
    // l'API Java (`ModuleCode.name().toLowerCase()`) et les deux applications lisent la même
    // colonne : elle ne peut pas être réécrite d'un seul côté.
    private static readonly Dictionary<string, ModuleCode> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["health"] = ModuleCode.Health,
        ["todolist"] = ModuleCode.Todolist,
        ["dofus"] = ModuleCode.Dofus,
        ["riot"] = ModuleCode.Riot,
        ["elite_dangerous"] = ModuleCode.EliteDangerous,
        ["palworld"] = ModuleCode.Palworld,
        ["temtem"] = ModuleCode.Temtem,
        ["codename"] = ModuleCode.Codename
    };

    // Un code inconnu ne doit jamais valoir un droit : il retourne null. Un module créé en base
    // et absent d'ici est donc simplement ignoré à la lecture du jeton — aucun use case ne peut
    // l'exiger tant qu'il n'est pas déclaré, l'énumération étant le seul moyen de le nommer.
    public static ModuleCode? Parse(string? code) =>
        code is not null && ByCode.TryGetValue(code, out var module) ? module : null;

    // Code tel qu'il est stocké en base. Construit depuis la même table que Parse : les deux
    // sens ne peuvent pas diverger.
    public static string ToCode(this ModuleCode module) =>
        ByCode.First(entry => entry.Value == module).Key;
}
