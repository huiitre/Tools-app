using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Net.Mail;
using System.Text.Json;

public sealed class ApiExceptionHandler(
    ApiProblemDetailsFactory problemDetailsFactory,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (status, code, title, detail, isTechnical) = exception switch
        {
            ApplicationException applicationException => MapApplicationException(applicationException),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "INVALID_REQUEST_BODY",
                "Bad Request",
                "Le corps de la requête est invalide.",
                false),
            System.Text.Json.JsonException => (
                StatusCodes.Status400BadRequest,
                "INVALID_REQUEST_BODY",
                "Bad Request",
                "Le corps de la requête est invalide.",
                false),
            NpgsqlException or TimeoutException => (
                StatusCodes.Status503ServiceUnavailable,
                "DEPENDENCY_UNAVAILABLE",
                "Service Unavailable",
                "Une dépendance nécessaire est indisponible.",
                true),
            SmtpException => (
                StatusCodes.Status503ServiceUnavailable,
                "MAIL_DELIVERY_UNAVAILABLE",
                "Service Unavailable",
                "Le service d’envoi d’emails est momentanément indisponible.",
                true),
            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "Internal Server Error",
                "Une erreur interne est survenue.",
                true)
        };

        if (isTechnical)
        {
            logger.LogError(
                exception,
                "Erreur HTTP {Status} code={Code} requestId={RequestId}",
                status,
                code,
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogInformation(
                "Erreur HTTP {Status} code={Code} requestId={RequestId}",
                status,
                code,
                httpContext.TraceIdentifier);
        }

        var problem = problemDetailsFactory.Create(httpContext, status, code, title, detail);

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.Headers["X-Request-Id"] = httpContext.TraceIdentifier;
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);

        return true;
    }

    private static (int Status, string Code, string Title, string Detail, bool IsTechnical)
        MapApplicationException(ApplicationException exception) => exception.Kind switch
        {
            ErrorKind.Validation => (
                StatusCodes.Status400BadRequest,
                exception.Code,
                "Bad Request",
                exception.Message,
                false),
            ErrorKind.NotFound => (
                StatusCodes.Status404NotFound,
                exception.Code,
                "Not Found",
                exception.Message,
                false),
            ErrorKind.Conflict => (
                StatusCodes.Status409Conflict,
                exception.Code,
                "Conflict",
                exception.Message,
                false),
            ErrorKind.Unauthorized => (
                StatusCodes.Status401Unauthorized,
                exception.Code,
                "Unauthorized",
                exception.Message,
                false),
            ErrorKind.Forbidden => (
                StatusCodes.Status403Forbidden,
                exception.Code,
                "Forbidden",
                exception.Message,
                false),
            ErrorKind.Unavailable => (
                StatusCodes.Status503ServiceUnavailable,
                exception.Code,
                "Service Unavailable",
                exception.Message,
                true),
            _ => throw new InvalidOperationException($"Type d'erreur non pris en charge : {exception.Kind}.")
        };
}
