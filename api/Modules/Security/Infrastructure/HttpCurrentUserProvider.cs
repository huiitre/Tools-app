using System.Security.Claims;
using System.Text.Json;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Security.Infrastructure;

// Adaptateur HTTP du port ICurrentUserProvider : traduit les claims de la requête courante
// en CurrentUser.
//
// Il ne valide plus rien. La signature, l'issuer, l'expiration, le type de jeton et l'état
// du compte sont vérifiés une seule fois par requête par le middleware d'authentification ;
// tout ce qui arrive ici a déjà été accepté. Un ClaimsPrincipal non authentifié signifie
// donc simplement « appel anonyme », jamais « jeton refusé ».
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

            // Un code de rôle inconnu de l'énumération est ignoré plutôt que d'accorder un droit.
            var roles = ReadRoles(principal)
                .Select(RoleCodes.Parse)
                .Where(role => role is not null)
                .Select(role => role!.Value)
                .ToArray();

            return new CurrentUser(userId, roles, ReadModuleRoles(principal));
        }
    }

    // Le claim "roles" est écrit comme tableau JSON. Selon le handler qui a lu le jeton, il
    // arrive soit déplié en un claim par valeur, soit en un claim unique contenant le tableau
    // brut. Les deux formes sont acceptées : s'en remettre à une seule reviendrait à faire
    // dépendre les droits d'un détail d'implémentation de la bibliothèque JWT.
    private static IEnumerable<string> ReadRoles(ClaimsPrincipal principal)
    {
        foreach (var claim in principal.FindAll(JwtClaims.Roles))
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

    // Le claim "modules" est un objet JSON { code_module: [codes_rôle] } : les droits qui ne
    // valent qu'à l'intérieur d'un module. Une entrée dont le module ou le rôle est inconnu de
    // l'énumération est écartée — comme pour les rôles globaux, un code que le Core ne connaît
    // pas ne peut pas valoir un droit.
    //
    // La valeur d'un module est acceptée en chaîne ou en tableau : le claim a d'abord porté un
    // rôle unique par module, et un jeton de la forme précédente reste en circulation le temps
    // de sa validité.
    private static IReadOnlyDictionary<ModuleCode, IReadOnlyCollection<RoleCode>> ReadModuleRoles(
        ClaimsPrincipal principal)
    {
        var moduleRoles = new Dictionary<ModuleCode, IReadOnlyCollection<RoleCode>>();

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

                var roles = ReadModuleRoleCodes(value)
                    .Select(RoleCodes.Parse)
                    .Where(role => role is not null)
                    .Select(role => role!.Value)
                    .ToArray();

                if (roles.Length > 0)
                {
                    moduleRoles[module.Value] = roles;
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
}

public static class JwtClaims
{
    public const string Subject = "sub";
    public const string Roles = "roles";
    public const string Modules = "modules";
    public const string TokenType = "tokenType";
    public const string IsActive = "isActive";
}
