using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Infrastructure;

namespace Tools.Api.Modules.EliteDangerous;

public static class EliteDangerousModule
{
    public static IHostApplicationBuilder AddEliteDangerousModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IRouteImporter, SpanshJsonRouteImporter>();

        builder.Services.AddScoped<IExpeditionRepository, PostgresExpeditionRepository>();

        builder.Services.AddScoped<ListExpeditionsUseCase>();
        builder.Services.AddScoped<GetExpeditionUseCase>();
        builder.Services.AddScoped<ImportExpeditionUseCase>();
        builder.Services.AddScoped<UpdateProgressUseCase>();
        builder.Services.AddScoped<RenameExpeditionUseCase>();
        builder.Services.AddScoped<ExportExpeditionUseCase>();
        builder.Services.AddScoped<DeleteExpeditionUseCase>();

        return builder;
    }
}
