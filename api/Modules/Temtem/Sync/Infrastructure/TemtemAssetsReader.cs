using System.Net.Http.Json;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Temtem.Sync.Infrastructure;

// Lit les fichiers publiés par l'extracteur Temtem sur le CDN des assets. Tous sont des tableaux
// JSON, sauf version.json qui appartient à l'extracteur et n'est jamais lu ici.
public sealed class TemtemAssetsReader(HttpClient httpClient)
{
    public async Task<JsonElement> Read(string fileName)
    {
        try
        {
            var payload = await httpClient.GetFromJsonAsync<JsonElement>(fileName);

            return payload.ValueKind == JsonValueKind.Array
                ? payload.Clone()
                : throw AppException.Unavailable(
                    "TEMTEM_ASSET_INVALID",
                    $"Le fichier {fileName} ne contient pas un tableau JSON.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw AppException.Unavailable(
                "TEMTEM_ASSET_UNAVAILABLE",
                $"Le fichier {fileName} est indisponible : {exception.Message}");
        }
        catch (JsonException exception)
        {
            throw AppException.Unavailable(
                "TEMTEM_ASSET_INVALID",
                $"Le fichier {fileName} est illisible : {exception.Message}");
        }
    }
}
