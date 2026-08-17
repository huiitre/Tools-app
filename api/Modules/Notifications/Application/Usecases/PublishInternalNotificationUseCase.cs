using Tools.Api.Modules.Notifications.Application;
using Tools.Api.Modules.Notifications.Application.Services;

namespace Tools.Api.Modules.Notifications.Application.Usecases;

// Publication de notification déclenchée par un appel de service à service, sans utilisateur à autoriser.
public sealed class PublishInternalNotificationUseCase(NotificationService notificationService)
{
    public Task<long?> Execute(SendNotificationCommand command) => notificationService.Send(command);
}
