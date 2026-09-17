using Tools.Api.Modules.Core.AppLogs.Application.Ports;
using Tools.Api.Modules.Core.AppLogs.Application.Services;
using Tools.Api.Modules.Core.AppLogs.Application.Usecases;
using Tools.Api.Modules.Core.AppLogs.Infrastructure;

namespace Tools.Api.Modules.Core.AppLogs;

// Composition du journal applicatif transverse. Le module ne propose que l'écriture : la future
// consultation administrative sera ajoutée avec son propre use case, sans ouvrir de mutation.
public static class AppLogsModule
{
    public static IHostApplicationBuilder AddAppLogsModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAppLogRepository, PostgresAppLogRepository>();
        builder.Services.AddScoped<IAppLogContextProvider, HttpAppLogContextProvider>();
        builder.Services.AddScoped<AppLogService>();
        builder.Services.AddScoped<ListAppLogsUseCase>();

        return builder;
    }
}
