using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Infrastructure.Status;

// API REST officielle Palworld. Le port de la cible est le port REST de poll, pas le port de jeu.
public sealed class PalworldRestStatusProvider(HttpClient httpClient) : IGameServerStatusProvider
{
    public string ProtocolType => "PALWORLD_REST";

    public async Task<GameServerStatus> FetchAsync(GameServerPollTarget gameServer, CancellationToken cancellationToken)
    {
        try
        {
            var username = GameServerProtocolConfig.GetString(gameServer, "username");
            var password = GameServerProtocolConfig.GetString(gameServer, "password");
            if (string.IsNullOrWhiteSpace(username) || password is null)
            {
                return GameServerStatus.Offline;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"http://{gameServer.Host}:{gameServer.Port}/v1/api/metrics");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return GameServerStatus.Offline;
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var metrics = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var root = metrics.RootElement;
            return new GameServerStatus(
                true,
                ReadInteger(root, "currentplayernum"),
                ReadInteger(root, "maxplayernum"));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return GameServerStatus.Offline;
        }
        catch (Exception)
        {
            return GameServerStatus.Offline;
        }
    }

    private static int? ReadInteger(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
            ? number
            : null;
}
