using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Application.Usecases;
using Tools.Api.Modules.Core.AndroidApps.Infrastructure;

namespace Tools.Api.Modules.Core.AndroidApps;

// Apps Android tierces distribuées par huihuistore : import d'APK et choix de la version proposée
// depuis l'administration, catalogue public lu par les TV. Voir README.md.
public static class AndroidAppsModule
{
    public static IHostApplicationBuilder AddAndroidAppsModule(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AndroidAppsOptions>(builder.Configuration.GetSection(AndroidAppsOptions.SectionName));

        builder.Services.AddScoped<IAndroidAppRepository, PostgresAndroidAppRepository>();
        builder.Services.AddSingleton<IAndroidAppFileStore, FileSystemAndroidAppFileStore>();
        builder.Services.AddSingleton<IApkInspector, AndroidBuildToolsApkInspector>();
        builder.Services.AddSingleton<IAndroidAppAssetUrlBuilder, AndroidAppAssetUrlBuilder>();

        builder.Services.AddScoped<AnalyzeAndroidAppImportUseCase>();
        builder.Services.AddScoped<ConfirmAndroidAppImportUseCase>();
        builder.Services.AddScoped<CancelAndroidAppImportUseCase>();
        builder.Services.AddScoped<ListAndroidAppsUseCase>();
        builder.Services.AddScoped<UpdateAndroidAppUseCase>();
        builder.Services.AddScoped<DeleteAndroidAppReleaseUseCase>();
        builder.Services.AddScoped<DeleteAndroidAppUseCase>();
        builder.Services.AddScoped<GetAndroidAppCatalogueUseCase>();

        return builder;
    }
}
