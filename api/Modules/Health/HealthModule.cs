using Tools.ApiCore.Modules.Health.Application;
using Tools.ApiCore.Modules.Health.Infrastructure;

namespace Tools.ApiCore.Modules.Health;

// Composition du module Health : les sondes et l'identification du déploiement.
//
// `/version` vit ici plutôt que dans la racine de composition : c'est une route de diagnostic,
// de la même famille que `/health/*`, appelée par les mêmes clients sans jeton — les tests
// d'intégration les vérifient d'ailleurs ensemble (DiagnosticsTests).
public static class HealthModule
{
    public static IHostApplicationBuilder AddHealthModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IHealthRepository, PostgresHealthRepository>();
        builder.Services.AddScoped<CheckReadinessUseCase>();

        return builder;
    }

    // Interrogée par le healthcheck du conteneur et par Watchtower, qui ne présentent aucun
    // jeton : elle doit rester anonyme malgré l'authentification exigée par défaut.
    public static WebApplication MapVersionEndpoint(this WebApplication app)
    {
        var applicationVersion = app.Configuration["Application:Version"] ?? "unknown";
        var gitSha = app.Configuration["Application:GitSha"] ?? "unknown";

        app.MapGet("/version", () => new
        {
            service = "api",
            runtime = ".NET",
            version = applicationVersion,
            gitSha,
            environment = app.Environment.EnvironmentName
        }).AllowAnonymous();

        return app;
    }
}
