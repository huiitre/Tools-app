using Tools.Api.Modules.Realtime.Application.Ports;
using Tools.Api.Modules.Realtime.Application.Usecases;
using Tools.Api.Modules.Realtime.Infrastructure;

namespace Tools.Api.Modules.Realtime;

// Composition du module Realtime : le point de connexion WebSocket unique de l'application
// (SignalR) et le relais d'événements pour les appelants de service à service.
public static class RealtimeModule
{
    public const string HubRoute = "/hub";

    public static IHostApplicationBuilder AddRealtimeModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSignalR();

        builder.Services.AddScoped<IRecipientResolver, PostgresRecipientResolver>();
        builder.Services.AddScoped<IRealtimePublisher, SignalRRealtimePublisher>();
        builder.Services.AddScoped<PublishRealtimeEventUseCase>();

        return builder;
    }

    public static WebApplication MapRealtimeModule(this WebApplication app)
    {
        app.MapHub<CoreHub>(HubRoute);
        return app;
    }
}
