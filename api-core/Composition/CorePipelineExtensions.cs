using Tools.ApiCore.Modules.Common.Api.Errors;

namespace Tools.ApiCore.Composition;

// L'ordre du pipeline HTTP, au même endroit et dans un seul sens de lecture.
//
// C'est la principale raison de ne pas laisser chaque module ajouter ses propres middlewares :
// leur ordre relatif est un comportement, pas un détail d'organisation.
public static class CorePipelineExtensions
{
    public static WebApplication UseCorePipeline(this WebApplication app)
    {
        app.UseMiddleware<RequestIdMiddleware>();
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
}
