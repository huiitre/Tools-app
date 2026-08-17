using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tools.ApiCore.Modules.Common.Api.Errors;
using Tools.ApiCore.Modules.Realtime;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Jwt;

// Authentification du pipeline : « ce jeton est-il valide, et qui es-tu ? ».
//
// Ce middleware ne décide d'aucun droit — il ne rend jamais de 403. L'autorisation reste
// portée par les use cases (voir docs/SECURITY.md).
public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddCoreJwtAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // La configuration est résolue paresseusement, à la construction des options, et non
        // au moment de l'enregistrement : lire `IConfiguration` ici même figerait les valeurs
        // avant que les sources ajoutées plus tard ne s'appliquent — celles des tests
        // d'intégration, notamment, qui seraient alors silencieusement ignorées au profit des
        // variables d'environnement de la machine.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration, IOptions<JwtOptions>>((options, configuration, jwtOptions) =>
            {
                // Sans cela, .NET renomme "sub" en ClaimTypes.NameIdentifier et les lectures
                // du claim standard reviennent vides.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = JwtTokenParameters.Validation(
                    jwtOptions.Value,
                    configuration["JWT_SECRET"] ?? string.Empty);
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ReadTokenFromQueryStringForHub,
                    OnTokenValidated = EnforceAccessTokenRules,
                    OnChallenge = WriteUnauthorizedProblem,
                    OnForbidden = WriteForbiddenProblem
                };
            });

        services.AddAuthorization(options =>
        {
            // Secure by default : toute route sans déclaration exige un appelant authentifié.
            // Avec l'opt-in inverse ([Authorize] posé au cas par cas), un oubli ouvre une route
            // en silence ; ici, un oubli la ferme et se voit à la première requête.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }

    // Le navigateur ne peut pas poser d'en-tête personnalisé sur la poignée de main WebSocket :
    // le token voyage donc en query string pour cette seule route, jamais ailleurs.
    private static Task ReadTokenFromQueryStringForHub(MessageReceivedContext context)
    {
        if (context.HttpContext.Request.Path.StartsWithSegments(RealtimeModule.HubRoute))
        {
            context.Token = context.Request.Query["access_token"];
        }

        return Task.CompletedTask;
    }

    // Le middleware valide signature, issuer et expiration — rien de plus. Ces deux règles
    // étaient portées par ITokenService.ReadAccessToken et doivent être reconduites ici,
    // sinon elles disparaissent en même temps que l'ancien chemin de lecture.
    private static Task EnforceAccessTokenRules(TokenValidatedContext context)
    {
        var principal = context.Principal;

        // Access et refresh tokens partagent secret, issuer et algorithme : sans ce contrôle,
        // un refresh token présenté en Bearer vaudrait un accès de sept jours.
        if (principal?.FindFirstValue("tokenType") != "ACCESS")
        {
            context.Fail(new SecurityTokenException("Le jeton présenté n'est pas un access token."));
            return Task.CompletedTask;
        }

        // Un compte désactivé conserve un jeton signé valide jusqu'à son expiration.
        if (principal.FindFirstValue("isActive") != "true")
        {
            context.Fail(new SecurityTokenException("Le compte associé au jeton est désactivé."));
        }

        return Task.CompletedTask;
    }

    private static Task WriteUnauthorizedProblem(JwtBearerChallengeContext context)
    {
        // Empêche le middleware d'écrire sa propre réponse : le contrat d'erreur est unique.
        context.HandleResponse();

        // Deux situations distinctes que le front traite différemment : personne ne s'est
        // présenté, ou quelqu'un s'est présenté avec un jeton que l'on refuse.
        var presentedAToken = context.Request.Headers.Authorization
            .ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        return presentedAToken
            ? WriteProblem(context.HttpContext, StatusCodes.Status401Unauthorized,
                "INVALID_ACCESS_TOKEN", "Unauthorized", "Session invalide ou expirée.")
            : WriteProblem(context.HttpContext, StatusCodes.Status401Unauthorized,
                "UNAUTHENTICATED", "Unauthorized", "Authentification requise.");
    }

    // La FallbackPolicy n'exige qu'un utilisateur authentifié : ce cas ne devrait pas se
    // produire tant qu'aucune policy de rôle n'est déclarée sur une route. Il est branché
    // pour qu'un 403 du pipeline ne puisse jamais échapper au contrat d'erreur.
    private static Task WriteForbiddenProblem(ForbiddenContext context) =>
        WriteProblem(context.HttpContext, StatusCodes.Status403Forbidden,
            "FORBIDDEN", "Forbidden", "Accès refusé.");

    private static async Task WriteProblem(
        HttpContext httpContext,
        int status,
        string code,
        string title,
        string message)
    {
        var problem = httpContext.RequestServices
            .GetRequiredService<ApiProblemDetailsFactory>()
            .Create(httpContext, status, code, title, message);

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.Headers["X-Request-Id"] = httpContext.TraceIdentifier;
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
