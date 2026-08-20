using Tools.Api.Modules.Core.GameServers.Application;
using Tools.Api.Modules.Core.GameServers.Application.Ports;
using Tools.Api.Modules.Core.GameServers.Infrastructure;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Polling;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Status;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Microsoft.Extensions.Options;

namespace Tools.Api.Modules.Core.GameServers;

// Composition du flux de manifest et du poll de statut. Les adapters sont choisis uniquement par
// protocol_type, jamais par gameCode.
public static class GameServersModule
{
    public static IHostApplicationBuilder AddGameServersModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<PostgresGameServerRepository>();
        builder.Services.AddScoped<IGameServerRepository>(services => services.GetRequiredService<PostgresGameServerRepository>());
        builder.Services.AddScoped<IGameServerPollingRepository>(services => services.GetRequiredService<PostgresGameServerRepository>());
        builder.Services.AddScoped<IGameServerDashboardRepository>(services => services.GetRequiredService<PostgresGameServerRepository>());
        builder.Services.AddSingleton<IGameServerImageUrlBuilder, GameServerImageUrlBuilder>();
        builder.Services.AddHttpClient<IGameServersManifestProvider, GameServersManifestProvider>((services, client) =>
        {
            var appOptions = services.GetRequiredService<IOptions<AppOptions>>().Value;
            client.BaseAddress = new Uri($"{appOptions.AssetsBaseUrl.TrimEnd('/')}/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        builder.Services.AddHttpClient<ISteamAppDetailsProvider, SteamAppDetailsProvider>(client =>
        {
            client.BaseAddress = new Uri("https://store.steampowered.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Tools-GameServers/1.0");
        });
        builder.Services.AddScoped<GameServersSyncUseCase>();
        builder.Services.AddHttpClient<PalworldRestStatusProvider>(client => client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddSingleton<IGameServerStatusProvider, SteamA2sStatusProvider>();
        builder.Services.AddTransient<IGameServerStatusProvider, PalworldRestStatusProvider>();
        builder.Services.AddSingleton<IGameServerStatusProvider, SourceRconStatusProvider>();
        builder.Services.AddSingleton<IGameServerStatusProvider, HumanitZRconStatusProvider>();
        builder.Services.AddScoped<PollGameServersUseCase>();
        builder.Services.AddScoped<GetGameServersUseCase>();

        if (!builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddHostedService<GameServersPollingService>();
        }

        return builder;
    }
}
