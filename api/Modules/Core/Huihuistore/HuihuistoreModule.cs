using Tools.Api.Modules.Core.Huihuistore.Application.Ports;
using Tools.Api.Modules.Core.Huihuistore.Application.Usecases;
using Tools.Api.Modules.Core.Huihuistore.Infrastructure;

namespace Tools.Api.Modules.Core.Huihuistore;

// Mise à jour de huihuistore lui-même : sa CI publie l'APK, l'app lit la version publiée. Réutilise
// la lecture d'APK et le stockage de AndroidApps, qui doit être enregistré avant. Voir README.md.
public static class HuihuistoreModule
{
    public static IHostApplicationBuilder AddHuihuistoreModule(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<HuihuistoreOptions>(builder.Configuration.GetSection(HuihuistoreOptions.SectionName));

        builder.Services.AddSingleton<IHuihuistoreReleaseStore, FileSystemHuihuistoreReleaseStore>();

        builder.Services.AddScoped<PublishHuihuistoreReleaseUseCase>();
        builder.Services.AddScoped<GetHuihuistoreUpdateUseCase>();

        return builder;
    }
}
