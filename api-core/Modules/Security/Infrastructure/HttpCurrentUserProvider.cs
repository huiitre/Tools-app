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

            return new CurrentUser(userId, roles);
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
}

public static class JwtClaims
{
    public const string Subject = "sub";
    public const string Roles = "roles";
    public const string TokenType = "tokenType";
    public const string IsActive = "isActive";
}
