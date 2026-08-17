using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tools.Api.Modules.Common.Api.Errors;
using Tools.Api.Modules.Common.Application.Exceptions;
using Tools.Api.Modules.Common.Application.Ports;
using Tools.Api.Modules.Common.Infrastructure;

namespace Tools.Api.Modules.Common;

// Composition du module Common : le contrat d'erreur HTTP et l'accès PostgreSQL partagé.
//
// Ce module n'a pas de métier ; il porte ce dont tous les autres dépendent. Il est donc le
// premier enregistré, et il ne doit jamais dépendre d'un module métier — la flèche va
// toujours dans l'autre sens.
public static class CommonModule
{
    public static IHostApplicationBuilder AddCommonModule(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));

        AddErrorContract(builder.Services);
        AddPostgres(builder.Services, builder.Configuration);

        return builder;
    }

    // Contrat d'erreur unique : application/problem+json, produit par une seule fabrique.
    // Voir docs/ARCHITECTURE.md, section « Contrat d'erreur HTTP ».
    private static void AddErrorContract(IServiceCollection services)
    {
        services.AddSingleton<ApiProblemDetailsFactory>();
        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var problemDetailsFactory = context.HttpContext.RequestServices
                    .GetRequiredService<ApiProblemDetailsFactory>();

                problemDetailsFactory.Enrich(context.ProblemDetails, context.HttpContext);
            };
        });

        // La validation automatique de [ApiController] passe par la même fabrique, sinon elle
        // écrirait son propre format d'erreur. Configurer ApiBehaviorOptions directement plutôt
        // que par ConfigureApiBehaviorOptions rend cet enregistrement indépendant de l'endroit
        // où AddControllers() est appelé : le contrat d'erreur appartient à ce module, pas à
        // la racine de composition.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetailsFactory = context.HttpContext.RequestServices
                    .GetRequiredService<ApiProblemDetailsFactory>();

                return new BadRequestObjectResult(
                    problemDetailsFactory.CreateValidation(context.HttpContext, context.ModelState));
            };
        });
    }

    private static void AddPostgres(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = PostgresConnectionString.FromEnvironmentVariables(configuration)
            ?? configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string Postgres manquante");

        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddScoped<PostgresSession>();
        services.AddScoped<ITransactionManager, PostgresTransactionManager>();
    }

    // Endpoints de vérification du contrat d'erreur, mappés uniquement en environnement
    // Testing par la racine de composition. Ils vérifient le contrat, pas l'authentification :
    // d'où l'anonymat.
    public static WebApplication MapErrorContractTestingEndpoints(this WebApplication app)
    {
        app.MapGet("/_tests/errors/{kind}", (string kind) =>
        {
            throw kind switch
            {
                "validation" => AppException.Validation(
                    "TEST_VALIDATION_ERROR",
                    "Erreur de validation de test."),
                "not-found" => AppException.NotFound(
                    "TEST_NOT_FOUND_ERROR",
                    "Ressource de test introuvable."),
                "conflict" => AppException.Conflict(
                    "TEST_CONFLICT_ERROR",
                    "Conflit de test."),
                "forbidden" => AppException.Forbidden(
                    "TEST_FORBIDDEN_ERROR",
                    "Accès refusé pour le test."),
                "unavailable" => AppException.Unavailable(
                    "TEST_UNAVAILABLE_ERROR",
                    "Dépendance indisponible pour le test."),
                "internal" => throw new InvalidOperationException("Erreur technique de test."),
                _ => throw AppException.Validation(
                    "TEST_UNKNOWN_ERROR_KIND",
                    "Type d'erreur de test inconnu.")
            };
        }).AllowAnonymous();

        return app;
    }
}
