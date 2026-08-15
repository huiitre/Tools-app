using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Auth.Application.Ports.Google;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Google;

public sealed class GoogleOAuthClient(
    HttpClient httpClient,
    IOptions<GoogleOAuthOptions> options,
    IConfiguration configuration) : IGoogleOAuthClient
{
    private readonly GoogleOAuthOptions options = options.Value;
    private readonly string clientId = configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
    private readonly string clientSecret = configuration["GOOGLE_CLIENT_SECRET"] ?? string.Empty;

    public string BuildAuthorizationUrl(string state)
    {
        EnsureConfigured();
        return QueryHelpers.AddQueryString(
            "https://accounts.google.com/o/oauth2/v2/auth",
            new Dictionary<string, string?>
            {
                ["client_id"] = clientId,
                ["redirect_uri"] = options.RedirectUri,
                ["response_type"] = "code",
                ["scope"] = "openid email profile",
                ["state"] = state,
                ["access_type"] = "online"
            });
    }

    public async Task<string> ExchangeCodeForIdTokenAsync(string code)
    {
        EnsureConfigured();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = options.RedirectUri,
            ["grant_type"] = "authorization_code"
        });

        using var response = await httpClient.PostAsync("token", content);
        if (!response.IsSuccessStatusCode)
        {
            throw AppException.Unauthorized("GOOGLE_AUTHENTICATION_FAILED", "Authentification Google invalide.");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        return !string.IsNullOrWhiteSpace(tokenResponse?.IdToken)
            ? tokenResponse.IdToken
            : throw AppException.Unauthorized("GOOGLE_AUTHENTICATION_FAILED", "Authentification Google invalide.");
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret)
            || string.IsNullOrWhiteSpace(options.RedirectUri)
            || string.IsNullOrWhiteSpace(options.FrontendBaseUrl))
        {
            throw new InvalidOperationException("La configuration Google OAuth est incomplète.");
        }
    }

    private sealed record GoogleTokenResponse([property: JsonPropertyName("id_token")] string? IdToken);
}
