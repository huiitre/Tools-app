using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

namespace Tools.Api.Composition;

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
        builder.Services.AddHttpContextAccessor();

        // X-Forwarded-For ne doit être accepté que depuis le reverse proxy connu. Sans cette
        // liste, un client pourrait forger cet en-tête et usurper l'adresse enregistrée dans le
        // journal applicatif. Une liste vide signifie volontairement « ne faire confiance à
        // personne » : l'adresse TCP directe reste alors utilisée.
        var forwardedHeaders = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            RequireHeaderSymmetry = true
        };
        forwardedHeaders.KnownIPNetworks.Clear();
        forwardedHeaders.KnownProxies.Clear();

        foreach (var value in builder.Configuration.GetSection("ReverseProxy:TrustedProxies").Get<string[]>() ?? [])
        {
            if (!IPAddress.TryParse(value, out var address))
            {
                throw new InvalidOperationException($"ReverseProxy:TrustedProxies contient une adresse IP invalide : '{value}'.");
            }

            forwardedHeaders.KnownProxies.Add(address);
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = forwardedHeaders.ForwardedHeaders;
            options.ForwardLimit = forwardedHeaders.ForwardLimit;
            options.RequireHeaderSymmetry = forwardedHeaders.RequireHeaderSymmetry;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var proxy in forwardedHeaders.KnownProxies) options.KnownProxies.Add(proxy);
        });

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
