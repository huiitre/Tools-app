using Tools.Api.Modules.Core.AppLogs.Application.Ports;
using Tools.Api.Modules.Core.AppLogs.Application.Services;
using Tools.Api.Modules.Core.AppLogs.Application.Usecases;
using Tools.Api.Modules.Core.AppLogs.Infrastructure;

namespace Tools.Api.Modules.Core.AppLogs;

// Composition du journal applicatif transverse : écriture (AppLogService) et lecture administrative
// (ListAppLogsUseCase), toujours sans aucun chemin de mutation ou de suppression du journal.
public static class AppLogsModule
{
    public static IHostApplicationBuilder AddAppLogsModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAppLogRepository, PostgresAppLogRepository>();
        builder.Services.AddScoped<IAppLogContextProvider, HttpAppLogContextProvider>();
        builder.Services.AddSingleton<IGeoIpLookup, MaxMindGeoIpLookup>();
        builder.Services.AddScoped<AppLogService>();
        builder.Services.AddScoped<ListAppLogsUseCase>();

        return builder;
    }
}
