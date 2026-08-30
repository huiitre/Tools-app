using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Temtem.Sync.Application.Ports;
using Tools.Api.Modules.Temtem.Sync.Application.Usecases;
using Tools.Api.Modules.Temtem.Sync.Infrastructure;
using Tools.Api.Modules.Temtem.Creatures.Application.Ports;
using Tools.Api.Modules.Temtem.Creatures.Application.Usecases;
using Tools.Api.Modules.Temtem.Creatures.Infrastructure;
using Tools.Api.Modules.Temtem.Types.Application.Ports;
using Tools.Api.Modules.Temtem.Types.Application.Usecases;
using Tools.Api.Modules.Temtem.Types.Infrastructure;
using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Usecases;
using Tools.Api.Modules.Temtem.Teams.Infrastructure;

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

        // Catalogue en lecture — un sous-module par entité, comme chez Dofus.
        builder.Services.AddScoped<ITemtemTypeRepository, PostgresTemtemTypeRepository>();
        builder.Services.AddScoped<ITemtemCreatureRepository, PostgresTemtemCreatureRepository>();
        builder.Services.AddScoped<ListTemtemTypesUseCase>();
        builder.Services.AddScoped<ListTemtemCreaturesUseCase>();
        builder.Services.AddScoped<GetTemtemBySlugUseCase>();

        // Équipes de l'utilisateur — la seule partie du module qui écrit.
        builder.Services.AddScoped<ITemtemTeamRepository, PostgresTemtemTeamRepository>();
        builder.Services.AddScoped<ListMyTemtemTeamsUseCase>();
        builder.Services.AddScoped<CreateTemtemTeamUseCase>();
        builder.Services.AddScoped<RenameTemtemTeamUseCase>();
        builder.Services.AddScoped<DeleteTemtemTeamUseCase>();
        builder.Services.AddScoped<AddTemtemTeamMemberUseCase>();
        builder.Services.AddScoped<ReorderTemtemTeamMembersUseCase>();
        builder.Services.AddScoped<RemoveTemtemTeamMemberUseCase>();
        builder.Services.AddScoped<SetTemtemTeamMemberTechniquesUseCase>();

        return builder;
    }
}
