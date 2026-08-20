using Tools.Api.Modules.Core.Notifications.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Notifications.Application.Usecases;

// Envoi manuel d'une notification, déclenché par un humain.
//
// TECH, comme dans l'API Java : c'est un outil d'exploitation, pas une action de produit. À ne
// pas confondre avec `PublishInternalNotificationUseCase`, qui sert les appels de service à
// service et n'a aucun utilisateur à autoriser.
public sealed class SendNotificationUseCase(
    UseCaseAuthorizer authorizer,
    NotificationService notificationService) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Tech;

    // Retourne l'identifiant créé, nul si la cible n'a désigné aucun destinataire.
    public Task<long?> Execute(SendNotificationCommand command)
    {
        return notificationService.Send(command);
    }
}
