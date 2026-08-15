using Tools.ApiCore.Modules.Notifications.Application.Ports;
using Tools.ApiCore.Modules.Notifications.Application.Services;
using Tools.ApiCore.Modules.Notifications.Infrastructure;

namespace Tools.ApiCore.Modules.Notifications;

// Composition du module Notifications.
//
// Socle uniquement : persistance et résolution des destinataires. Les routes de lecture
// (`GET /notifications`, marquage lu, suppression) et la livraison temps réel restent servies
// par l'API Java ; les deux écrivent dans les mêmes tables. Le module s'y substituera avec la
// tranche SignalR, sans que ce qui est ici ait à changer.
public static class NotificationsModule
{
    public static IHostApplicationBuilder AddNotificationsModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<INotificationRepository, PostgresNotificationRepository>();
        builder.Services.AddScoped<NotificationService>();

        return builder;
    }
}
