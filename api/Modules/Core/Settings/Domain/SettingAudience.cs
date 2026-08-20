using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Pour qui on résout : l'utilisateur, son rôle global et ses rôles de module.
//
// C'est une projection de `CurrentUser`, pas une copie par commodité : le Domain de Settings ne
// doit pas dépendre de la couche Application d'un autre module. Le use case fait la traduction,
// et la résolution reste testable sans jeton ni requête HTTP.
public sealed record SettingAudience(
    long? UserId,
    RoleCode? GlobalRole,
    IReadOnlyDictionary<ModuleCode, RoleCode> ModuleRoles)
{
    // Aucun appelant : une tâche de fond. Seules les valeurs globales s'appliquent — aucune
    // ligne de rôle ni d'utilisateur ne peut la viser.
    public static readonly SettingAudience None =
        new(null, null, new Dictionary<ModuleCode, RoleCode>());

    // Rôle à comparer pour un paramètre donné. Un paramètre de module se juge sur le rôle
    // **dans ce module**, jamais sur le rôle global : c'est la règle de `UseCaseAuthorizer`,
    // reprise à l'identique. Un utilisateur absent du module n'a aucun rôle ici, donc ne voit
    // pas le paramètre.
    public RoleCode? RoleFor(SettingDefinition definition) =>
        definition.Module is null
            ? GlobalRole
            : ModuleRoles.TryGetValue(definition.Module.Value, out var role) ? role : null;

    // Tous les codes de rôle que cette audience porte, global et modules confondus. Sert à
    // borner la requête SQL : on ramène large, la résolution retrie exactement.
    public IReadOnlyCollection<string> AllRoleCodes() =>
        [.. ModuleRoles.Values
            .Append(GlobalRole ?? default)
            .Where(role => role != default)
            .Distinct()
            .Select(role => role.ToCode())];

    // Identité stable de l'audience, pour mémoriser ses lignes le temps d'une requête. Le record
    // ne peut pas servir de clé : il contient un dictionnaire, dont l'égalité est celle de la
    // référence — deux audiences identiques ne seraient jamais reconnues comme telles.
    public string CacheKey =>
        $"{UserId}|{GlobalRole}|{string.Join(',', ModuleRoles.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}:{entry.Value}"))}";
}
