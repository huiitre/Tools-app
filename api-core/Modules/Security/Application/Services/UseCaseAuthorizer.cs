using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Domain;
using Tools.ApiCore.Modules.Common.Application.Exceptions;

namespace Tools.ApiCore.Modules.Security.Application.Services;

// Applique la règle d'accès d'un use case. Les rôles sont lus dans l'access token :
// aucune requête n'est faite ici, au prix d'un droit retiré qui ne s'applique qu'au
// renouvellement du token. Contrairement à l'aspect Java, un appel sans utilisateur
// identifié est refusé et non laissé passer : le Core n'a aucune protection au niveau
// des routes qui prendrait le relais.
public sealed class UseCaseAuthorizer(
    ICurrentUserProvider currentUserProvider,
    ILogger<UseCaseAuthorizer> logger)
{
    // Retourne l'appelant validé : le use case y accède sans avoir à le résoudre lui-même.
    public CurrentUser EnsureAtLeast(RoleCode requiredRole)
    {
        var currentUser = currentUserProvider.Current;
        if (currentUser is null)
        {
            throw AppException.Unauthorized(
                "UNAUTHENTICATED",
                "Authentification requise.");
        }

        var actualRole = currentUser.HighestRole;
        if (actualRole is null || !actualRole.Value.HasAtLeast(requiredRole))
        {
            // Trace la tentative sans révéler à l'appelant le rôle attendu.
            logger.LogWarning(
                "Accès refusé userId={UserId} requiredRole={RequiredRole} actualRole={ActualRole}",
                currentUser.UserId,
                requiredRole,
                actualRole);

            throw AppException.Forbidden(
                "INSUFFICIENT_ROLE",
                "Vous n’avez pas les droits nécessaires pour cette action.");
        }

        return currentUser;
    }
}
