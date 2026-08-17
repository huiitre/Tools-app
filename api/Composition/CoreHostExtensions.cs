using Serilog;

namespace Tools.ApiCore.Composition;

// Ce qui appartient à l'hôte et à aucun module : configuration, journalisation, MVC, CORS.
//
// La règle de partage est simple — si retirer un module rendait l'enregistrement inutile, il
// appartient à ce module et non ici.
public static class CoreHostExtensions
{
    public const string CorsPolicyName = "ToolsFrontend";

    public static WebApplicationBuilder AddCoreHost(this WebApplicationBuilder builder)
    {
        // Surcharge locale non versionnée : elle n'existe sur aucun environnement déployé.
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        builder.Services.AddControllers();

        // AllowCredentials est nécessaire au cookie de refresh, posé sur un autre sous-domaine
        // que le front. Il interdit le joker sur les origines : la liste est donc explicite,
        // par environnement.
        builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        return builder;
    }
}
