using Dapper;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;
using Tools.Api.Modules.Riot.Sync.Application.Usecases;
using Tools.Api.Modules.Riot.Sync.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Services;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.User.Services;
using Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;
using Tools.Api.Modules.Riot.Valorant.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Infrastructure.Scheduling;

namespace Tools.Api.Modules.Riot;

public static class RiotModule
{
    public static IHostApplicationBuilder AddRiotModule(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<RiotOptions>(builder.Configuration.GetSection(RiotOptions.SectionName));

        // TOOLS_ENCRYPTION_KEY est déjà fournie à tous les environnements pour l'API Java, et les
        // deux APIs lisent les mêmes lignes chiffrées : la reprendre évite d'avoir à provisionner
        // un second secret qui devrait de toute façon porter la même valeur.
        builder.Services.PostConfigure<RiotOptions>(options =>
            options.EncryptionMasterKey = string.IsNullOrWhiteSpace(options.EncryptionMasterKey)
                ? builder.Configuration["TOOLS_ENCRYPTION_KEY"] ?? string.Empty
                : options.EncryptionMasterKey);

        AddInfrastructure(builder);
        AddCatalogUseCases(builder.Services);
        AddAccountUseCases(builder.Services);
        AddUserUseCases(builder.Services);
        AddSyncUseCases(builder.Services);

        return builder;
    }

    private static void AddInfrastructure(IHostApplicationBuilder builder)
    {
        // Réglage global de Dapper, sans lequel toute écriture portant une date sans heure échoue.
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        builder.Services.AddScoped<RiotDatabase>();
        builder.Services.AddSingleton<ValorantAssetUrlBuilder>();

        // Les fichiers de l'extracteur sont lus en HTTP sur le CDN des assets, comme le manifest
        // des serveurs de jeux — l'API Java les lisait sur un montage disque du NAS.
        builder.Services.AddHttpClient<ValorantAssetsReader>((services, client) =>
        {
            var appOptions = services.GetRequiredService<IOptions<AppOptions>>().Value;
            client.BaseAddress = new Uri($"{appOptions.AssetsBaseUrl.TrimEnd('/')}/tools_riot/valorant/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Riot répond lentement quand ses services sont chargés, sans jamais dépasser la minute.
        builder.Services.AddHttpClient<IRiotAuthPort, RiotAuthHttpAdapter>(
            client => client.Timeout = TimeSpan.FromSeconds(20));
        builder.Services.AddHttpClient<IValorantStorePort, ValorantStoreHttpAdapter>(
            client => client.Timeout = TimeSpan.FromSeconds(20));

        builder.Services.AddSingleton<IValorantTokenParser, ValorantTokenParser>();
        builder.Services.AddSingleton<IValorantTokenCipher, AesGcmValorantTokenCipher>();
        builder.Services.AddScoped<IValorantVersionProvider, ValorantAssetsVersionProvider>();

        builder.Services.AddScoped<IValorantAuthRepository, PostgresValorantAuthRepository>();
        builder.Services.AddScoped<IValorantBundleRepository, PostgresValorantBundleRepository>();
        builder.Services.AddScoped<IValorantWeaponRepository, PostgresValorantWeaponRepository>();
        builder.Services.AddScoped<IValorantSkinRepository, PostgresValorantSkinRepository>();
        builder.Services.AddScoped<IValorantUserSkinRepository, PostgresValorantUserSkinRepository>();
        builder.Services.AddScoped<IValorantWatchlistRepository, PostgresValorantWatchlistRepository>();
        builder.Services.AddScoped<IValorantStoreHistoryRepository, PostgresValorantStoreHistoryRepository>();

        builder.Services.AddScoped<IValorantContentTierDataProvider, ValorantAssetsContentTierDataProvider>();
        builder.Services.AddScoped<IValorantWeaponDataProvider, ValorantAssetsWeaponDataProvider>();
        builder.Services.AddScoped<IValorantSkinDataProvider, ValorantAssetsSkinDataProvider>();
        builder.Services.AddScoped<IValorantBundleDataProvider, ValorantAssetsBundleDataProvider>();

        builder.Services.AddScoped<IValorantContentTierSyncRepository, PostgresValorantContentTierSyncRepository>();
        builder.Services.AddScoped<IValorantWeaponSyncRepository, PostgresValorantWeaponSyncRepository>();
        builder.Services.AddScoped<IValorantSkinSyncRepository, PostgresValorantSkinSyncRepository>();
        builder.Services.AddScoped<IValorantSkinLevelSyncRepository, PostgresValorantSkinLevelSyncRepository>();
        builder.Services.AddScoped<IValorantSkinChromaSyncRepository, PostgresValorantSkinChromaSyncRepository>();
        builder.Services.AddScoped<IValorantBundleSyncRepository, PostgresValorantBundleSyncRepository>();

        builder.Services.AddScoped<ValorantAuthService>();
        builder.Services.AddScoped<ValorantWatchlistNotifier>();
        builder.Services.AddHostedService<ValorantWatchlistSchedulerService>();
    }

    private static void AddCatalogUseCases(IServiceCollection services)
    {
        services.AddScoped<ListValorantBundlesUseCase>();
        services.AddScoped<GetValorantBundleUseCase>();
        services.AddScoped<GetValorantBundleByAssetIdUseCase>();
        services.AddScoped<ListValorantWeaponsUseCase>();
        services.AddScoped<GetValorantWeaponUseCase>();
        services.AddScoped<GetValorantWeaponSkinsUseCase>();
        services.AddScoped<ListValorantSkinsUseCase>();
        services.AddScoped<ListValorantSkinsByThemeUseCase>();
        services.AddScoped<GetValorantSkinUseCase>();
        services.AddScoped<GetValorantSkinByAssetIdUseCase>();
        services.AddScoped<GetValorantSkinByLevelUseCase>();
        services.AddScoped<GetValorantStoreUseCase>();
    }

    private static void AddAccountUseCases(IServiceCollection services)
    {
        services.AddScoped<GetValorantVersionUseCase>();
        services.AddScoped<ListValorantAccountsUseCase>();
        services.AddScoped<LinkValorantAccountUseCase>();
        services.AddScoped<RenameValorantAccountUseCase>();
        services.AddScoped<UnlinkValorantAccountUseCase>();
        services.AddScoped<GetValorantAccessTokenUseCase>();
    }

    private static void AddUserUseCases(IServiceCollection services)
    {
        services.AddScoped<GetMyValorantUserSkinsUseCase>();
        services.AddScoped<AddMyValorantSkinUseCase>();
        services.AddScoped<RemoveMyValorantSkinUseCase>();
        services.AddScoped<GetMyValorantWatchlistUseCase>();
        services.AddScoped<AddSkinToWatchlistUseCase>();
        services.AddScoped<RemoveSkinFromWatchlistUseCase>();
        services.AddScoped<GetMyValorantStoreHistoryUseCase>();
        services.AddScoped<AddSkinToStoreHistoryUseCase>();
        services.AddScoped<TriggerValorantWatchlistSyncUseCase>();
    }

    private static void AddSyncUseCases(IServiceCollection services)
    {
        services.AddScoped<SyncValorantContentTiersUseCase>();
        services.AddScoped<SyncValorantWeaponsUseCase>();
        services.AddScoped<SyncValorantSkinsUseCase>();
        services.AddScoped<SyncValorantBundlesUseCase>();
        services.AddScoped<SyncValorantUseCase>();
    }
}
