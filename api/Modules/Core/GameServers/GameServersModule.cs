using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Listing;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Polling;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;
using Tools.Api.Modules.Core.GameServers.Application.Usecases;
using Tools.Api.Modules.Core.GameServers.Infrastructure;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Persistence;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Polling;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Sync;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Microsoft.Extensions.Options;

namespace Tools.Api.Modules.Core.GameServers;

// Composition du flux de manifest et du poll de statut. Les adapters sont choisis uniquement par
// protocol_type, jamais par gameCode.
public static class GameServersModule
{
    public static IHostApplicationBuilder AddGameServersModule(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<GameServersOptions>(builder.Configuration.GetSection(GameServersOptions.SectionName));
        var hostOverride = builder.Configuration[$"{GameServersOptions.SectionName}:{nameof(GameServersOptions.HostOverride)}"];
        hostOverride = string.IsNullOrWhiteSpace(hostOverride) ? null : hostOverride;

        builder.Services.AddScoped<PostgresGameServerRepository>();
        builder.Services.AddScoped<IGameServerRepository>(services => services.GetRequiredService<PostgresGameServerRepository>());
        builder.Services.AddScoped<IGameServerPollingRepository>(services => hostOverride is null
            ? services.GetRequiredService<PostgresGameServerRepository>()
            : new HostOverridingGameServerPollingRepository(services.GetRequiredService<PostgresGameServerRepository>(), hostOverride));
        builder.Services.AddScoped<IGameServerDashboardRepository>(services => services.GetRequiredService<PostgresGameServerRepository>());
        builder.Services.AddScoped<IGameServerTargetRepository>(services => hostOverride is null
            ? services.GetRequiredService<PostgresGameServerRepository>()
            : new HostOverridingGameServerTargetRepository(services.GetRequiredService<PostgresGameServerRepository>(), hostOverride));
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

        // Un fichier par jeu, résolu par gameCode : le scheduler et le dashboard passent tous
        // deux par là. Un jeu absent d'ici n'est pas pollé ; un jeu qui n'implémente pas en plus
        // IGameServerDashboard n'a pas de dashboard. Les clients ci-dessous sont les briques de
        // transport qu'ils partagent, sans connaissance d'aucun jeu.
        builder.Services.AddSingleton<SteamA2sClient>();
        builder.Services.AddSingleton<HumanitzRconClient>();
        builder.Services.AddHttpClient<PalworldProvider>(client => client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddTransient<IGameServerProvider>(services => services.GetRequiredService<PalworldProvider>());
        builder.Services.AddSingleton<IGameServerProvider, ArkProvider>();
        builder.Services.AddSingleton<IGameServerProvider, RustProvider>();
        builder.Services.AddSingleton<IGameServerProvider, SevenDaysToDieProvider>();
        builder.Services.AddSingleton<IGameServerProvider, HumanitzProvider>();

        builder.Services.AddScoped<PollGameServersUseCase>();
        builder.Services.AddScoped<GetGameServersUseCase>();
        builder.Services.AddScoped<GetGameServerDashboardUseCase>();

        // Les tests n'ont jamais de poll de fond. En dev il ne tourne que si un hôte de
        // substitution est configuré : sans lui les cibles sont des IP docker injoignables depuis
        // le poste, et chaque passage écraserait les statuts clonés par des « hors ligne ».
        var pollingEnabled = !builder.Environment.IsEnvironment("Testing")
                             && (!builder.Environment.IsDevelopment() || hostOverride is not null);
        if (pollingEnabled)
        {
            builder.Services.AddHostedService<GameServersPollingService>();
        }

        return builder;
    }
}
