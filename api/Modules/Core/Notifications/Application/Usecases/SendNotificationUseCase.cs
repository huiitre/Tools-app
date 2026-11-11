using Tools.Api.Modules.Core.Notifications.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Notifications.Application.Usecases;

// Envoi d'une notification, par un humain (TECH : outil d'exploitation, pas action de produit)
// ou par un service muni d'une clé d'API portant core.notifications:publish.
public sealed class SendNotificationUseCase(
    UseCaseAuthorizer authorizer,
    NotificationService notificationService) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Tech;
    protected override ApiKeyScope? RequiredScope => ApiKeyScope.CoreNotificationsPublish;

    // Retourne l'identifiant créé, nul si la cible n'a désigné aucun destinataire.
    public Task<long?> Execute(SendNotificationCommand command)
    {
        return notificationService.Send(command);
    }
}
