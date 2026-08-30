using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Temtem.Sync.Application.Ports;
using Tools.Api.Modules.Temtem.Sync.Application.Usecases;
using Tools.Api.Modules.Temtem.Sync.Infrastructure;

namespace Tools.Api.Modules.Temtem;

public static class TemtemModule
{
    public static IHostApplicationBuilder AddTemtemModule(this IHostApplicationBuilder builder)
    {
        // Les données de l'extracteur sont lues en HTTP sur le CDN des assets, comme le manifest
        // des serveurs de jeux : aucun montage disque à prévoir.
        builder.Services.AddHttpClient<TemtemAssetsReader>((services, client) =>
        {
            var appOptions = services.GetRequiredService<IOptions<AppOptions>>().Value;
            client.BaseAddress = new Uri($"{appOptions.AssetsBaseUrl.TrimEnd('/')}/tools_temtem/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddSingleton<TemtemAssetUrlBuilder>();
        builder.Services.AddScoped<ITemtemDataProvider, TemtemAssetsDataProvider>();
        builder.Services.AddScoped<ITemtemCatalogueRepository, PostgresTemtemCatalogueRepository>();
        builder.Services.AddScoped<SyncTemtemCatalogueUseCase>();

        return builder;
    }
}
