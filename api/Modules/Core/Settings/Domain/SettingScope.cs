namespace Tools.Api.Modules.Core.Settings.Domain;

// À quoi une valeur de paramètre est accrochée.
//
// Trois accroches possibles, et une seule par valeur stockée :
//   Global — le site entier      « par défaut, tout le monde est en thème sombre »
//   Role   — un rôle             « les modérateurs sont en thème clair »
//   User   — une personne        « moi, je suis en clair »
//
// **Les valeurs portent la priorité** : la plus précise gagne, donc `Global < Role < User`.
// La résolution compare des entiers, il n'y a pas de table de priorités à tenir à côté —
// même principe que `RoleCode`, dont la valeur porte le niveau hiérarchique.
public enum SettingScope
{
    Global = 1,

    // Correspondance **exacte** : une valeur posée sur MODERATOR s'applique aux modérateurs et
    // à personne d'autre. Ce n'est pas un seuil — contrairement aux rôles minimum d'une
    // définition, qui sont des permissions et se comparent avec `>=`.
    Role = 2,

    User = 3
}

// Les accroches qu'un paramètre accepte. Un paramètre d'instance ne déclare que `Global` : la
// notion de valeur par personne n'existe pas pour lui.
public static class SettingScopes
{
    public static readonly IReadOnlySet<SettingScope> All =
        new HashSet<SettingScope> { SettingScope.Global, SettingScope.Role, SettingScope.User };

    public static readonly IReadOnlySet<SettingScope> GlobalOnly =
        new HashSet<SettingScope> { SettingScope.Global };

    public static readonly IReadOnlySet<SettingScope> WithoutUser =
        new HashSet<SettingScope> { SettingScope.Global, SettingScope.Role };
}
