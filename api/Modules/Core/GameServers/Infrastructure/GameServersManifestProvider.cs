using System.Net.Http.Json;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.GameServers.Application.Dto;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure;

// Lit le résultat atomique de l'extractor sur le serveur d'assets. Contrairement à Steam,
// l'indisponibilité de ce fichier doit faire échouer le sync : un tableau inconnu ne doit jamais
// être interprété comme un scan vide et déclencher des suppressions.
public sealed class GameServersManifestProvider(HttpClient httpClient) : IGameServersManifestProvider
{
    public async Task<IReadOnlyList<GameServerSyncDto>> FetchAsync()
    {
        try
        {
            var manifests = await httpClient.GetFromJsonAsync<List<GameServerSyncDto>>(
                "tools_core/gameservers/gameservers.json");

            return manifests ?? throw AppException.Unavailable(
                "GAME_SERVERS_MANIFEST_UNAVAILABLE",
                "Le manifest des serveurs de jeux est vide ou indisponible.");
        }
        catch (HttpRequestException exception)
        {
            throw AppException.Unavailable(
                "GAME_SERVERS_MANIFEST_UNAVAILABLE",
                $"Le manifest des serveurs de jeux est indisponible : {exception.Message}");
        }
        catch (TaskCanceledException exception)
        {
            throw AppException.Unavailable(
                "GAME_SERVERS_MANIFEST_UNAVAILABLE",
                $"La lecture du manifest des serveurs de jeux a expiré : {exception.Message}");
        }
        catch (JsonException exception)
        {
            throw AppException.Unavailable(
                "GAME_SERVERS_MANIFEST_INVALID",
                $"Le manifest des serveurs de jeux est invalide : {exception.Message}");
        }
    }
}
