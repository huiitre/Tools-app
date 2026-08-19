using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Infrastructure;

// Client du Store Steam : une erreur réseau est distincte d'une application inconnue. Dans le
// premier cas, le sync conserve les métadonnées existantes ; dans le second, elles deviennent null.
public sealed class SteamAppDetailsProvider(
    HttpClient httpClient,
    ILogger<SteamAppDetailsProvider> logger) : ISteamAppDetailsProvider
{
    public async Task<SteamAppDetailsLookup> FindAsync(int steamAppId)
    {
        var url = QueryHelpers.AddQueryString(
            "api/appdetails",
            new Dictionary<string, string?>
            {
                ["appids"] = steamAppId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["l"] = "french"
            });

        try
        {
            using var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Steam AppDetails a refusé l'application {SteamAppId} avec HTTP {StatusCode}.",
                    steamAppId, (int)response.StatusCode);
                return new SteamAppDetailsLookup(false, null, null);
            }

            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, SteamAppDetailsEnvelope>>();
            if (payload is null || !payload.TryGetValue(steamAppId.ToString(), out var envelope) || !envelope.Success)
            {
                // Steam a répondu correctement mais ne connaît pas cet App ID : cette absence est
                // une donnée autoritaire, contrairement à un timeout où l'on conserve l'existant.
                return SteamAppDetailsLookup.AvailableWithoutMetadata;
            }

            return new SteamAppDetailsLookup(true, envelope.Data?.Name, envelope.Data?.HeaderImage);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Steam AppDetails est indisponible pour l'application {SteamAppId}.", steamAppId);
            return new SteamAppDetailsLookup(false, null, null);
        }
        catch (TaskCanceledException exception)
        {
            logger.LogWarning(exception, "Steam AppDetails a expiré pour l'application {SteamAppId}.", steamAppId);
            return new SteamAppDetailsLookup(false, null, null);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Steam AppDetails a répondu un JSON invalide pour l'application {SteamAppId}.", steamAppId);
            return new SteamAppDetailsLookup(false, null, null);
        }
    }

    private sealed record SteamAppDetailsEnvelope(bool Success, SteamAppDetailsData? Data);

    private sealed record SteamAppDetailsData(
        string? Name,
        [property: JsonPropertyName("header_image")] string? HeaderImage);
}
