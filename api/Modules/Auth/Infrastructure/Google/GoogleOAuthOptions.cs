using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Google;

// Configuration non sensible versionnée ; GOOGLE_CLIENT_ID et GOOGLE_CLIENT_SECRET viennent de l'environnement.
public sealed class GoogleOAuthOptions
{
    public const string SectionName = "Google:OAuth";
    public string RedirectUri { get; init; } = string.Empty;
    public string FrontendBaseUrl { get; init; } = string.Empty;
}
