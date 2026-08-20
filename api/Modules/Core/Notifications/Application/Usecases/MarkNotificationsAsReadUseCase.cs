using Tools.Api.Modules.Core.Notifications.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.Notifications.Application.Usecases;

// Marque comme lues les notifications de l'appelant.
public sealed class MarkNotificationsAsReadUseCase(
    UseCaseAuthorizer authorizer,
    INotificationRepository notificationRepository) : SecuredUseCase(authorizer)
{
    // `notificationIds` nul : tout est marqué lu. L'appelant ne peut agir que sur ses propres
    // lignes — le filtre porte sur `CurrentUser.UserId`, jamais sur une valeur reçue du client.
    public Task Execute(IReadOnlyCollection<long>? notificationIds)
    {
        return notificationRepository.MarkAsReadAsync(CurrentUser.UserId, notificationIds);
    }
}
