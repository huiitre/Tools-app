using System.Net.Http.Json;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Services;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

// Échange le refresh token contre un access token, sur le point d'entrée OAuth de Riot.
public sealed class RiotAuthHttpAdapter(
    HttpClient httpClient,
    IValorantTokenParser tokenParser) : IRiotAuthPort
{
    private const string TokenUrl = "https://auth.riotgames.com/token";
    private const string ClientId = "prod-xsso-playvalorant";

    // Riot ne dit pas combien de temps vit le refresh token qu'il rend. Trente jours est la durée
    // observée, reprise telle quelle du Java : la colonne expires_at n'est qu'indicative, rien ne
    // s'appuie dessus pour décider d'un renouvellement.
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<IRiotAuthPort.ValorantAuthResponse> Refresh(string refreshToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await httpClient.PostAsync(TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = ClientId
            }));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw AppException.Unavailable(
                "RIOT_AUTH_UNAVAILABLE",
                $"Le service d'authentification Riot est injoignable : {exception.Message}");
        }

        // Riot refuse un refresh token périmé par un 4xx. Ce code déclenche la suppression du
        // compte lié en amont : il ne doit pas être posé sur une panne réseau ou un 5xx.
        if ((int)response.StatusCode is >= 400 and < 500)
        {
            throw AppException.Validation(
                ValorantAuthService.TokenInvalidCode,
                "Riot a refusé ce refresh token : il faut relier le compte.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw AppException.Unavailable(
                "RIOT_AUTH_UNAVAILABLE",
                $"Le service d'authentification Riot a répondu {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();

        var accessToken = ReadString(payload, "access_token");
        var newRefreshToken = ReadString(payload, "refresh_token");

        return new IRiotAuthPort.ValorantAuthResponse(
            accessToken,
            newRefreshToken,
            tokenParser.ExtractPuuid(accessToken),
            DateTime.UtcNow.Add(RefreshTokenLifetime));
    }

    private static string ReadString(JsonElement payload, string propertyName) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty(propertyName, out var property)
        && property.GetString() is { Length: > 0 } value
            ? value
            : throw AppException.Unavailable(
                "RIOT_AUTH_EMPTY_RESPONSE",
                $"La réponse de Riot ne contient pas « {propertyName} ».");
}
