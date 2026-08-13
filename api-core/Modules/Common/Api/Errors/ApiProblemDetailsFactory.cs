using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public sealed class ApiProblemDetailsFactory
{
    public ProblemDetails Create(
        HttpContext httpContext,
        int status,
        string code,
        string title,
        string message)
    {
        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Instance = httpContext.Request.Path
        };

        AddExtensions(problem, httpContext, code, message);

        return problem;
    }

    public ValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "La valeur fournie est invalide."
                        : error.ErrorMessage)
                    .ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Title = "Requête invalide.",
            Status = StatusCodes.Status400BadRequest,
            Instance = httpContext.Request.Path
        };

        AddExtensions(
            problem,
            httpContext,
            "VALIDATION_FAILED",
            "Une ou plusieurs valeurs sont invalides.");

        return problem;
    }

    public void Enrich(ProblemDetails problem, HttpContext httpContext)
    {
        problem.Type = null;

        if (!problem.Extensions.ContainsKey("code"))
        {
            var code = GetDefaultCode(problem.Status);
            problem.Extensions["code"] = code;
        }

        if (!problem.Extensions.ContainsKey("message"))
        {
            problem.Extensions["message"] = GetDefaultMessage(problem.Status);
        }

        if (!problem.Extensions.ContainsKey("requestId"))
        {
            problem.Extensions["requestId"] = httpContext.TraceIdentifier;
        }

        if (System.Diagnostics.Activity.Current?.Id is { } traceId
            && !problem.Extensions.ContainsKey("traceId"))
        {
            problem.Extensions["traceId"] = traceId;
        }
    }

    private void AddExtensions(
        ProblemDetails problem,
        HttpContext httpContext,
        string code,
        string message)
    {
        problem.Extensions["code"] = code;
        problem.Extensions["message"] = message;
        Enrich(problem, httpContext);
    }

    private static string GetDefaultCode(int? status) => status switch
    {
        StatusCodes.Status400BadRequest => "BAD_REQUEST",
        StatusCodes.Status401Unauthorized => "UNAUTHENTICATED",
        StatusCodes.Status403Forbidden => "FORBIDDEN",
        StatusCodes.Status404NotFound => "ROUTE_NOT_FOUND",
        StatusCodes.Status405MethodNotAllowed => "METHOD_NOT_ALLOWED",
        StatusCodes.Status409Conflict => "CONFLICT",
        StatusCodes.Status415UnsupportedMediaType => "UNSUPPORTED_MEDIA_TYPE",
        StatusCodes.Status429TooManyRequests => "TOO_MANY_REQUESTS",
        StatusCodes.Status500InternalServerError => "INTERNAL_ERROR",
        StatusCodes.Status503ServiceUnavailable => "SERVICE_UNAVAILABLE",
        _ => "HTTP_ERROR"
    };

    private static string GetDefaultMessage(int? status) => status switch
    {
        StatusCodes.Status400BadRequest => "La requête est invalide.",
        StatusCodes.Status401Unauthorized => "Authentification requise.",
        StatusCodes.Status403Forbidden => "Accès refusé.",
        StatusCodes.Status404NotFound => "La route demandée est introuvable.",
        StatusCodes.Status405MethodNotAllowed => "La méthode HTTP n'est pas autorisée.",
        StatusCodes.Status409Conflict => "La requête entre en conflit avec l'état actuel.",
        StatusCodes.Status415UnsupportedMediaType => "Le type de contenu n'est pas pris en charge.",
        StatusCodes.Status429TooManyRequests => "Trop de requêtes ont été envoyées.",
        StatusCodes.Status500InternalServerError => "Une erreur interne est survenue.",
        StatusCodes.Status503ServiceUnavailable => "Le service est temporairement indisponible.",
        _ => "Une erreur HTTP est survenue."
    };
}
