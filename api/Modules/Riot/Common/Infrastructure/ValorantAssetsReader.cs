using System.Net.Http.Json;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Riot.Common.Infrastructure;

// Lit les fichiers de données Valorant déposés sur le NAS par l'extracteur.
//
// **L'API Java les lisait sur disque** (`tools.assets.base-path`, un montage du NAS dans le
// conteneur). Ici ils sont lus en HTTP depuis AssetsBaseUrl : c'est la bascule disque→HTTP déjà
// décidée, et le chemin que suit déjà GameServersManifestProvider. Aucun montage à prévoir, et
// le poste de développement lit la même source que la production.
//
// Tous ces fichiers ont la même forme : un objet racine avec une propriété `data`.
public sealed class ValorantAssetsReader(HttpClient httpClient)
{
    public async Task<JsonElement> ReadDataNode(string fileName)
    {
        try
        {
            var payload = await httpClient.GetFromJsonAsync<JsonElement>(fileName);

            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("data", out var data))
            {
                throw AppException.Unavailable(
                    "VALORANT_ASSET_INVALID",
                    $"Le fichier {fileName} ne contient pas de bloc « data ».");
            }

            return data.Clone();
        }
        catch (HttpRequestException exception)
        {
            throw AppException.Unavailable(
                "VALORANT_ASSET_UNAVAILABLE",
                $"Le fichier {fileName} est indisponible : {exception.Message}");
        }
        catch (TaskCanceledException exception)
        {
            throw AppException.Unavailable(
                "VALORANT_ASSET_UNAVAILABLE",
                $"La lecture de {fileName} a expiré : {exception.Message}");
        }
        catch (JsonException exception)
        {
            throw AppException.Unavailable(
                "VALORANT_ASSET_INVALID",
                $"Le fichier {fileName} est illisible : {exception.Message}");
        }
    }
}
