using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Tools.ApiCore.Modules.Common.Api.Errors;

namespace Tools.ApiCore.Modules.Common.Api.RateLimiting;

// Limitation de débit des routes anonymes qui déclenchent un envoi d'email.
//
// `/auth/register` et `/auth/password/reset-request` sont ouvertes et provoquent chacune un
// mail sortant. Sans limite, un appel en boucle épuise le quota SMTP et dégrade la réputation
// du domaine — au point que les mails légitimes finissent en spam.
//
// Ces routes ne créent aucun accès : un compte non confirmé ne peut pas se connecter et le
// nettoyage l'efface. Ce qu'on protège ici est le service d'envoi, pas les données.
public static class RateLimitingExtensions
{
    public const string EmailSendingPolicy = "email-sending";

    private const int PermitLimit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public static IServiceCollection AddCoreRateLimiting(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        // La politique est toujours déclarée : une route qui référencerait une politique
        // inexistante ferait échouer le démarrage. En test, elle ne limite simplement rien —
        // les requêtes en mémoire n'ont pas d'adresse IP et partageraient toutes le même
        // compteur, ce qui ferait échouer les tests enchaînant plusieurs appels.
        var unlimited = environment.IsEnvironment("Testing");

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(EmailSendingPolicy, context => unlimited
                ? RateLimitPartition.GetNoLimiter("testing")
                : RateLimitPartition.GetFixedWindowLimiter(
                    // Derrière le reverse proxy, l'IP réelle suppose que les en-têtes
                    // transmis sont pris en compte ; sans cela toutes les requêtes
                    // partagent la même partition.
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PermitLimit,
                        Window = Window
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var problem = httpContext.RequestServices
                    .GetRequiredService<ApiProblemDetailsFactory>()
                    .Create(
                        httpContext,
                        StatusCodes.Status429TooManyRequests,
                        "TOO_MANY_REQUESTS",
                        "Too Many Requests",
                        "Trop de demandes ont été envoyées. Réessayez dans quelques minutes.");

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.ContentType = "application/problem+json";
                httpContext.Response.Headers["X-Request-Id"] = httpContext.TraceIdentifier;

                // Indique au client quand réessayer, quand la fenêtre le permet.
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                await JsonSerializer.SerializeAsync(
                    httpContext.Response.Body,
                    problem,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    cancellationToken);
            };
        });

        return services;
    }
}
