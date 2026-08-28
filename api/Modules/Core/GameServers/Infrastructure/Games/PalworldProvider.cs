using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// API REST d'administration de Palworld. Le port de la cible est celui du REST, pas celui du jeu.
// Les identifiants viennent de protocol_config : ni l'URL ni le mot de passe ne sont écrits ici.
public sealed class PalworldProvider(HttpClient httpClient) : IGameServerProvider, IGameServerDashboard
{
    // Le monde fait 459 unités de sauvegarde par unité de carte, avec une translation et une
    // inversion des axes. Repris tel quel du module Palworld existant.
    private const double TranslationX = 123888;
    private const double TranslationY = 158000;
    private const double Scale = 459;

    public string GameCode => "PALWORLD";

    public async Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        try
        {
            using var metrics = await GetAsync(target, "metrics", cancellationToken);
            if (metrics is null)
            {
                return GameServerStatus.Offline;
            }

            return new GameServerStatus(
                true,
                ReadInt(metrics.RootElement, "currentplayernum"),
                ReadInt(metrics.RootElement, "maxplayernum"));
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

    public async Task<GameServerDetailsView> FetchDetailsAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        using var info = await GetAsync(target, "info", cancellationToken);
        var root = info?.RootElement;

        return new GameServerDetailsView(
            // Le nom du manifest fait foi, pour que la popup et la carte du widget concordent.
            target.ServerName,
            target.GameName,
            target.PictureUrl,
            root is null ? null : ReadString(root.Value, "version"),
            root is null ? null : ReadString(root.Value, "description"),
            root is null ? null : ReadString(root.Value, "worldguid"));
    }

    public async Task<GameServerLiveView> FetchLiveAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        // Trois appels concurrents plutôt qu'à la suite : ils visent le même serveur et ne
        // dépendent pas les uns des autres.
        var metricsTask = GetAsync(target, "metrics", cancellationToken);
        var playersTask = GetAsync(target, "players", cancellationToken);
        var gameDataTask = GetAsync(target, "game-data", cancellationToken);
        await Task.WhenAll(metricsTask, playersTask, gameDataTask);

        using var metrics = await metricsTask;
        using var players = await playersTask;
        using var gameData = await gameDataTask;

        var unavailable = new List<string>();
        if (metrics is null)
        {
            unavailable.Add("metrics");
        }

        if (gameData is null)
        {
            unavailable.Add("players");
        }

        var metricsRoot = metrics?.RootElement;
        return new GameServerLiveView(
            metricsRoot is null ? null : ReadInt(metricsRoot.Value, "currentplayernum"),
            metricsRoot is null ? null : ReadInt(metricsRoot.Value, "maxplayernum"),
            metricsRoot is null ? null : ReadDouble(metricsRoot.Value, "serverfps"),
            metricsRoot is null ? null : ReadDouble(metricsRoot.Value, "serverfpsaverage"),
            metricsRoot is null ? null : ReadDouble(metricsRoot.Value, "serverframetime"),
            metricsRoot is null ? null : ReadLong(metricsRoot.Value, "uptime"),
            metricsRoot is null ? null : ReadInt(metricsRoot.Value, "days"),
            metricsRoot is null ? null : ReadInt(metricsRoot.Value, "basecampnum"),
            ParsePlayers(gameData, players),
            // Palworld n'expose aucun journal serveur.
            [],
            unavailable);
    }

    // Le ping n'existe que dans /players, le reste que dans /game-data : les deux sources sont
    // rapprochées par userId.
    private static IReadOnlyList<GameServerLivePlayer> ParsePlayers(JsonDocument? gameData, JsonDocument? players)
    {
        if (gameData is null || !gameData.RootElement.TryGetProperty("ActorData", out var actors))
        {
            return [];
        }

        var pings = new Dictionary<string, int>(StringComparer.Ordinal);
        if (players is not null && players.RootElement.TryGetProperty("players", out var lightPlayers))
        {
            foreach (var player in lightPlayers.EnumerateArray())
            {
                var userId = ReadString(player, "userId");
                var ping = ReadDouble(player, "ping");
                if (userId is not null && ping is not null)
                {
                    pings[userId] = (int)Math.Round(ping.Value);
                }
            }
        }

        var companions = actors.EnumerateArray()
            .Where(actor => ReadString(actor, "Type") == "Character" && ReadString(actor, "UnitType") == "OtomoPal")
            .ToLookup(actor => ReadString(actor, "OwnerPlayerUId") ?? string.Empty, StringComparer.Ordinal);

        var result = new List<GameServerLivePlayer>();
        foreach (var actor in actors.EnumerateArray())
        {
            if (ReadString(actor, "Type") != "Character" || ReadString(actor, "UnitType") != "Player")
            {
                continue;
            }

            var userId = ReadString(actor, "userid");
            var (mapX, mapY) = ToMapPoint(ReadDouble(actor, "location_x"), ReadDouble(actor, "location_y"));
            var companion = companions[userId ?? string.Empty].FirstOrDefault();

            result.Add(new GameServerLivePlayer(
                ReadString(actor, "name") ?? "?",
                userId,
                userId is not null && pings.TryGetValue(userId, out var ping) ? ping : null,
                ReadInt(actor, "level"),
                ReadInt(actor, "hp"),
                ReadInt(actor, "max_hp"),
                ReadString(actor, "GuildName"),
                mapX,
                mapY,
                companion.ValueKind == JsonValueKind.Undefined
                    ? null
                    : new GameServerLiveCompanion(
                        ReadString(companion, "name") ?? "?",
                        ReadInt(companion, "level"),
                        ReadInt(companion, "hp"),
                        ReadInt(companion, "max_hp"))));
        }

        return result;
    }

    private static (double? X, double? Y) ToMapPoint(double? x, double? y)
    {
        if (x is null || y is null)
        {
            return (null, null);
        }

        // Axes volontairement inversés.
        return (Math.Round((y.Value - TranslationY) / Scale), Math.Round((x.Value + TranslationX) / Scale));
    }

    private async Task<JsonDocument?> GetAsync(GameServerTarget target, string route, CancellationToken cancellationToken)
    {
        var username = GameServerProtocolConfig.GetString(target.ProtocolConfig, "username");
        var password = GameServerProtocolConfig.GetString(target.ProtocolConfig, "password");
        if (string.IsNullOrWhiteSpace(username) || password is null)
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{target.Host}:{target.Port}/v1/api/{route}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int? ReadInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : null;

    private static long? ReadLong(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : null;

    private static double? ReadDouble(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetDouble(out var number) ? number : null;
}
