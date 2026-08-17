using Tools.Api.Modules.Notifications.Application.Ports;
using Tools.Api.Modules.Notifications.Application.Services;
using Tools.Api.Modules.Notifications.Application.Usecases;
using Tools.Api.Modules.Notifications.Infrastructure;

namespace Tools.Api.Modules.Notifications;

// Composition du module Notifications.
//
// Le module est complet depuis la migration du 2026-08-17 : écriture, résolution des
// destinataires, livraison temps réel (SignalR) et lecture/gestion pour l'utilisateur. Les
// tables restent celles de l'API Java, qui n'écrit plus que par la route interne
// (`/internal/notifications`) depuis ses propres flux métier.
public static class NotificationsModule
{
    public static IHostApplicationBuilder AddNotificationsModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<INotificationRepository, PostgresNotificationRepository>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<PublishInternalNotificationUseCase>();
        builder.Services.AddScoped<GetMyNotificationsUseCase>();
        builder.Services.AddScoped<MarkNotificationsAsReadUseCase>();
        builder.Services.AddScoped<DeleteNotificationsUseCase>();
        builder.Services.AddScoped<SendNotificationUseCase>();

        return builder;
    }
}
