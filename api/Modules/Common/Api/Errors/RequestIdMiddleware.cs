using Serilog.Context;

namespace Tools.ApiCore.Modules.Common.Api.Errors;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = GetRequestId(context.Request.Headers[HeaderName]);

        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await next(context);
        }
    }

    private static string GetRequestId(string? suppliedRequestId)
    {
        if (!string.IsNullOrWhiteSpace(suppliedRequestId)
            && suppliedRequestId.Length <= 128
            && suppliedRequestId.All(character => char.IsAsciiLetterOrDigit(character)
                || character is '.' or '_' or '-'))
        {
            return suppliedRequestId;
        }

        return Guid.NewGuid().ToString("N");
    }
}
