using Serilog;
using Serilog.Events;
using Tools.Api.Modules.Core.Common.Api.Errors;

namespace Tools.Api.Composition;

// L'ordre du pipeline HTTP, au même endroit et dans un seul sens de lecture.
//
// C'est la principale raison de ne pas laisser chaque module ajouter ses propres middlewares :
// leur ordre relatif est un comportement, pas un détail d'organisation.
public static class CorePipelineExtensions
{
    public static WebApplication UseCorePipeline(this WebApplication app)
    {
        app.UseMiddleware<RequestIdMiddleware>();

        // Une ligne par requête : méthode, chemin, statut et durée. Placée après
        // RequestIdMiddleware pour que l'identifiant de corrélation soit déjà posé, et avant
        // le reste pour englober ce qui échouerait plus loin dans le pipeline.
        app.UseSerilogRequestLogging(options => options.GetLevel = RequestLogLevel);

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors(CoreHostExtensions.CorsPolicyName);

        // L'ordre n'est pas négociable : l'authentification identifie l'appelant, l'autorisation
        // décide ensuite si la route lui est ouverte.
        app.UseAuthentication();

        // La FallbackPolicy s'applique aussi aux requêtes qui n'ont atteint aucun endpoint : sans
        // cette garde, une URL inconnue répondrait 401 au lieu de 404. Le front traite les 401 comme
        // une session expirée et tenterait un refresh sur une simple faute de frappe.
        app.Use(async (context, next) =>
        {
            if (context.GetEndpoint() is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });

        app.UseAuthorization();

        return app;
    }

    // Les sondes sont interrogées par le healthcheck du conteneur et par Watchtower toutes les
    // trente secondes. À `Information`, elles produiraient à elles seules des milliers de lignes
    // par jour et noieraient tout le reste : elles descendent donc à `Verbose`, c'est-à-dire
    // invisibles dans les niveaux configurés.
    private static LogEventLevel RequestLogLevel(HttpContext context, double elapsed, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        var path = context.Request.Path;
        var isDiagnostic = path.StartsWithSegments("/health") || path.StartsWithSegments("/version");

        return isDiagnostic ? LogEventLevel.Verbose : LogEventLevel.Information;
    }
}
