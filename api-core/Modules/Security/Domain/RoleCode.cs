namespace Tools.ApiCore.Modules.Security.Domain;

// Rôles globaux de tools_core.role, ordonnés du moins au plus permissif.
// La valeur de l'énumération porte le niveau hiérarchique : une comparaison suffit,
// il n'y a pas de table de niveaux à maintenir en parallèle de l'énumération.
public enum RoleCode
{
    ReadOnly = 1,
    User = 2,
    Moderator = 3,
    Tech = 4,
    Admin = 5,
    Owner = 6
}

public static class RoleCodes
{
    // Codes tels qu'ils sont stockés en base, insensibles à la casse.
    private static readonly Dictionary<string, RoleCode> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["READ_ONLY"] = RoleCode.ReadOnly,
        ["USER"] = RoleCode.User,
        ["MODERATOR"] = RoleCode.Moderator,
        ["TECH"] = RoleCode.Tech,
        ["ADMIN"] = RoleCode.Admin,
        ["OWNER"] = RoleCode.Owner
    };

    // Un code inconnu ne doit jamais valoir un droit : il retourne null.
    public static RoleCode? Parse(string? code) =>
        code is not null && ByCode.TryGetValue(code, out var role) ? role : null;

    public static bool HasAtLeast(this RoleCode actual, RoleCode required) => actual >= required;
}
