using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.ApiKeys.Application.Usecases;
using Tools.Api.Modules.Core.ApiKeys.Infrastructure;

namespace Tools.Api.Modules.Core.ApiKeys;

// Clés d'API des appelants de service à service : leur administration.
public static class ApiKeysModule
{
    public static IHostApplicationBuilder AddApiKeysModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IApiKeyRepository, PostgresApiKeyRepository>();
        builder.Services.AddScoped<CreateApiKeyUseCase>();
        builder.Services.AddScoped<RevokeApiKeyUseCase>();
        builder.Services.AddScoped<ListApiKeysUseCase>();
        builder.Services.AddScoped<ListApiKeyScopesUseCase>();

        return builder;
    }
}
