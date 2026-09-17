using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Ports;

namespace Tools.Api.Modules.Core.AppLogs.Infrastructure;

// Adaptateur HTTP du contexte d'un log. RemoteIpAddress est l'adresse vue par ASP.NET ; derrière
// le reverse proxy, elle ne deviendra l'adresse publique du client qu'après configuration sûre
// des forwarded headers dans le pipeline.
public sealed class HttpAppLogContextProvider(IHttpContextAccessor httpContextAccessor)
    : IAppLogContextProvider
{
    public AppLogContext Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return new AppLogContext(null, null);
            }

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            return new AppLogContext(
                httpContext.Connection.RemoteIpAddress,
                string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
        }
    }
}
