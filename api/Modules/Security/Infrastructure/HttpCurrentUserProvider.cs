using System.Security.Claims;
using System.Text.Json;
using Tools.Api.Modules.Security.Application.Ports;
using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Security.Infrastructure;

// Adaptateur HTTP du port ICurrentUserProvider : traduit les claims de la requête courante
// en CurrentUser.
//
// Il ne valide plus rien. La signature, l'issuer, l'expiration, le type de jeton et l'état
// du compte sont vérifiés une seule fois par requête par le middleware d'authentification ;
// tout ce qui arrive ici a déjà été accepté. Un ClaimsPrincipal non authentifié signifie
// donc simplement « appel anonyme », jamais « jeton refusé ».
//
// C'est le seul endroit qui tolère encore la forme plurielle des claims. Un utilisateur ne
// porte qu'un rôle global et qu'un rôle par module, mais des jetons émis avant ce
// resserrement restent valides le temps de leur durée de vie : la tolérance vit à la
// frontière, et le domaine derrière ne connaît qu'une valeur unique.
public sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public CurrentUser? Current
    {
        get
        {
            // Hors requête HTTP (tâche de fond), il n'y a aucun utilisateur.
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            if (!long.TryParse(principal.FindFirstValue(JwtClaims.Subject), out var userId))
            {
                return null;
            }

            return new CurrentUser(userId, ReadGlobalRole(principal), ReadModuleRoles(principal));
        }
    }

    // Rôle global. Le claim `role` porte une chaîne unique ; `roles`, un tableau, est la forme
    // antérieure et n'est lu que si le premier est absent. Un code inconnu de l'énumération est
    // ignoré plutôt que d'accorder un droit.
    private static RoleCode? ReadGlobalRole(ClaimsPrincipal principal)
    {
        var role = RoleCodes.Parse(principal.FindFirstValue(JwtClaims.Role));
        return role ?? Highest(ReadLegacyRoles(principal));
    }

    // Le claim `roles` était écrit comme tableau JSON. Selon le handler qui a lu le jeton, il
    // arrive soit déplié en un claim par valeur, soit en un claim unique contenant le tableau
    // brut : les deux formes sont acceptées.
    private static IEnumerable<string> ReadLegacyRoles(ClaimsPrincipal principal)
    {
        foreach (var claim in principal.FindAll(JwtClaims.LegacyRoles))
        {
            var value = claim.Value;
            if (!value.StartsWith('['))
            {
                yield return value;
                continue;
            }

            string[]? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<string[]>(value);
            }
            catch (JsonException)
            {
                // Un claim illisible ne vaut aucun rôle.
            }

            foreach (var role in parsed ?? [])
            {
                yield return role;
            }
        }
    }

    // Le claim `modules` est un objet JSON { code_module: code_rôle } : les droits qui ne
    // valent qu'à l'intérieur d'un module. Une entrée dont le module ou le rôle est inconnu de
    // l'énumération est écartée — comme pour le rôle global, un code que l'API ne connaît pas
    // ne peut pas valoir un droit.
    //
    // La valeur d'un module est acceptée en chaîne ou en tableau : le claim a porté un tableau
    // le temps où la base autorisait le cumul, et un jeton de cette forme reste en circulation
    // jusqu'à son expiration.
    private static IReadOnlyDictionary<ModuleCode, RoleCode> ReadModuleRoles(ClaimsPrincipal principal)
    {
        var moduleRoles = new Dictionary<ModuleCode, RoleCode>();

        foreach (var claim in principal.FindAll(JwtClaims.Modules))
        {
            Dictionary<string, JsonElement>? parsed = null;
            try
            {
                parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(claim.Value);
            }
            catch (JsonException)
            {
                // Un claim illisible ne vaut aucun droit de module.
            }

            foreach (var (moduleCode, value) in parsed ?? [])
            {
                var module = ModuleCodes.Parse(moduleCode);
                if (module is null)
                {
                    continue;
                }

                var role = Highest(ReadModuleRoleCodes(value));
                if (role is not null)
                {
                    moduleRoles[module.Value] = role.Value;
                }
            }
        }

        return moduleRoles;
    }

    private static IEnumerable<string> ReadModuleRoleCodes(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                yield return value.GetString()!;
                break;

            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        yield return item.GetString()!;
                    }
                }

                break;
        }
    }

    // Départage les formes plurielles héritées. Un jeton émis aujourd'hui ne porte jamais
    // qu'un code : cette méthode ne sert qu'à ne pas dégrader un droit déjà accordé.
    private static RoleCode? Highest(IEnumerable<string> codes)
    {
        RoleCode? highest = null;
        foreach (var code in codes)
        {
            var role = RoleCodes.Parse(code);
            if (role is not null && (highest is null || role > highest))
            {
                highest = role;
            }
        }

        return highest;
    }
}

public static class JwtClaims
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string Modules = "modules";
    public const string TokenType = "tokenType";
    public const string IsActive = "isActive";

    // Forme antérieure du rôle global, tableau JSON. Lue en repli tant que des jetons émis
    // avant le passage au rôle unique n'ont pas expiré ; à retirer ensuite.
    public const string LegacyRoles = "roles";
}
