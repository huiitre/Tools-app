using Tools.Api.Modules.GameServers.Application;
using Tools.Api.Modules.GameServers.Application.Ports;
using Tools.Api.Modules.GameServers.Infrastructure;

namespace Tools.Api.Modules.GameServers;

// Composition du flux interne de manifests : aucun poll de statut n'est enregistré ici pour
// l'instant. Il viendra avec les adapters de protocol_type dans un second lot.
public static class GameServersModule
{
    public static IHostApplicationBuilder AddGameServersModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IGameServerRepository, PostgresGameServerRepository>();
        builder.Services.AddSingleton<IGameServerImageUrlBuilder, GameServerImageUrlBuilder>();
        builder.Services.AddHttpClient<ISteamAppDetailsProvider, SteamAppDetailsProvider>(client =>
        {
            client.BaseAddress = new Uri("https://store.steampowered.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Tools-GameServers/1.0");
        });
        builder.Services.AddScoped<GameServersSyncUseCase>();

        return builder;
    }
}
