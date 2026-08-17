using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tools.Api.Modules.Common.Api.Internal;

// Réserve une route aux appels de service à service : un autre composant du NAS — l'API Java,
// un extracteur — qui n'agit au nom d'aucun utilisateur.
//
// La sécurité tient au secret partagé, jamais au réseau. Une route protégée uniquement parce
// qu'un `deny` est bien placé dans une configuration de reverse proxy redevient publique le
// jour où cette configuration est refaite ; ici, sans l'en-tête, l'appel échoue quelle que soit
// sa provenance. C'est le modèle des webhooks.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class InternalApiAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Internal-Token";
    private const string ConfigurationKey = "INTERNAL_API_TOKEN";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = configuration[ConfigurationKey];

        // Secret absent : la route est fermée, jamais ouverte. Une variable oubliée doit faire
        // échouer les appels internes, pas laisser entrer le premier venu.
        if (string.IsNullOrWhiteSpace(expected) || !IsAuthorized(context.HttpContext, expected))
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<InternalApiAttribute>();

            logger.LogWarning(
                "Appel interne refusé sur {Path} depuis {RemoteIp}.",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress);

            // 404 et non 401 : un 401 confirmerait l'existence de la route à qui la cherche.
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }

    private static bool IsAuthorized(HttpContext httpContext, string expected)
    {
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var provided))
        {
            return false;
        }

        // Comparaison à temps constant : un test d'égalité ordinaire s'arrête au premier octet
        // différent, et le temps de réponse révélerait alors le préfixe correct.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided.ToString()),
            Encoding.UTF8.GetBytes(expected));
    }
}
