using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.Security.Application.Services;

// Applique la règle d'accès d'un use case. Les rôles sont lus dans l'access token :
// aucune requête n'est faite ici, au prix d'un droit retiré qui ne s'applique qu'au
// renouvellement du token. Contrairement à l'aspect Java, un appel sans utilisateur
// identifié est refusé et non laissé passer : le Core n'a aucune protection au niveau
// des routes qui prendrait le relais.
public sealed class UseCaseAuthorizer(
    ICurrentUserProvider currentUserProvider,
    ILogger<UseCaseAuthorizer> logger)
{
    // Point d'entrée de SecuredUseCase : un utilisateur est jugé sur son rôle, une clé d'API sur
    // son scope. Sans `requiredScope`, toute clé est refusée.
    public Caller Authorize(RoleCode requiredRole, ModuleCode? requiredModule, ApiKeyScope? requiredScope)
    {
        var apiKey = currentUserProvider.CurrentApiKey;
        if (apiKey is null)
        {
            return new UserCaller(EnsureAtLeast(requiredRole, requiredModule));
        }

        if (requiredScope is null)
        {
            logger.LogWarning(
                "Clé d'API refusée apiKeyId={ApiKeyId} name={Name} : ce use case n'accepte aucune clé",
                apiKey.Id,
                apiKey.Name);

            throw AppException.Forbidden(
                "API_KEY_NOT_ALLOWED",
                "Cette action n'est pas ouverte aux clés d'API.");
        }

        if (!apiKey.Scopes.Contains(requiredScope.Value))
        {
            logger.LogWarning(
                "Clé d'API refusée apiKeyId={ApiKeyId} name={Name} requiredScope={RequiredScope}",
                apiKey.Id,
                apiKey.Name,
                requiredScope.Value.ToCode());

            throw AppException.Forbidden(
                "INSUFFICIENT_SCOPE",
                "Cette clé d'API n'a pas le droit nécessaire pour cette action.");
        }

        return new ApiKeyCaller(apiKey);
    }

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
            ? currentUser.Role
            : currentUser.RoleIn(requiredModule.Value);

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
