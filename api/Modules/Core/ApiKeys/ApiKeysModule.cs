using Microsoft.AspNetCore.Authentication;
using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.ApiKeys.Application.Services;
using Tools.Api.Modules.Core.ApiKeys.Application.Usecases;
using Tools.Api.Modules.Core.ApiKeys.Infrastructure;
using Tools.Api.Modules.Core.Security.Infrastructure;

namespace Tools.Api.Modules.Core.ApiKeys;

// Clés d'API des appelants de service à service : leur administration et l'authentification
// des appels qui en présentent une (schéma « ApiKey », choisi par le schéma par défaut de Auth).
public static class ApiKeysModule
{
    public static IHostApplicationBuilder AddApiKeysModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IApiKeyRepository, PostgresApiKeyRepository>();
        builder.Services.AddScoped<CreateApiKeyUseCase>();
        builder.Services.AddScoped<RevokeApiKeyUseCase>();
        builder.Services.AddScoped<ListApiKeysUseCase>();
        builder.Services.AddScoped<ListApiKeyScopesUseCase>();

        builder.Services.AddScoped<ApiKeyAuthenticator>();
        builder.Services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.SchemeName, null);

        return builder;
    }
}
