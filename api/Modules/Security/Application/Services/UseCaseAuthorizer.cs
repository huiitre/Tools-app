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
    //
    // `requiredModule` désigne le module auquel le use case appartient. Le rôle comparé est
    // alors celui détenu **dans ce module**, jamais le rôle global : sans cette règle, un
    // administrateur du site contournerait la restriction qu'on lui a posée sur un module, et
    // un droit de module ne voudrait plus rien dire pour les rôles élevés. Un use case sans
    // module reste jugé sur le rôle global.
    public CurrentUser EnsureAtLeast(RoleCode requiredRole, ModuleCode? requiredModule = null)
    {
        var currentUser = currentUserProvider.Current;
        if (currentUser is null)
        {
            throw AppException.Unauthorized(
                "UNAUTHENTICATED",
                "Authentification requise.");
        }

        var actualRole = requiredModule is null
            ? currentUser.HighestRole
            : currentUser.HighestRoleIn(requiredModule.Value);

        if (actualRole is null || !actualRole.Value.HasAtLeast(requiredRole))
        {
            // Trace la tentative sans révéler à l'appelant le rôle attendu.
            logger.LogWarning(
                "Accès refusé userId={UserId} module={Module} requiredRole={RequiredRole} actualRole={ActualRole}",
                currentUser.UserId,
                requiredModule,
                requiredRole,
                actualRole);

            // Deux refus distincts que le frontend ne traite pas de la même manière : le module
            // n'est pas ouvert à cet utilisateur, ou il l'est mais son rôle n'y suffit pas.
            throw requiredModule is not null && actualRole is null
                ? AppException.Forbidden(
                    "NO_MODULE_ACCESS",
                    "Ce module ne vous est pas ouvert.")
                : AppException.Forbidden(
                    "INSUFFICIENT_ROLE",
                    "Vous n’avez pas les droits nécessaires pour cette action.");
        }

        return currentUser;
    }
}
