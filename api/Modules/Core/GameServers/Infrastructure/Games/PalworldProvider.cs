using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// API REST d'administration de Palworld. Le port de la cible est celui du REST, pas celui du jeu.
// Les identifiants viennent de protocol_config : ni l'URL ni le mot de passe ne sont écrits ici.
public sealed class PalworldProvider(HttpClient httpClient) : IGameServerProvider, IGameServerDashboard, IGameServerActions
{
    // Le monde fait 459 unités de sauvegarde par unité de carte, avec une translation et une
    // inversion des axes. Repris tel quel du module Palworld existant.
    private const double TranslationX = 123888;
    private const double TranslationY = 158000;
    private const double Scale = 459;

    public string GameCode => "PALWORLD";

    public IReadOnlyList<GameServerActionDefinition> Actions { get; } =
    [
        new("announce", "Annoncer un message", "mdi-bullhorn-outline", RoleCode.Moderator, false,
            [new("message", "Message", "text", true, "Message à diffuser")]),
        new("save", "Sauvegarder le monde", "mdi-content-save-outline", RoleCode.Moderator, false, []),
        new("kick", "Expulser un joueur", "mdi-account-remove-outline", RoleCode.Moderator, false,
            [new("userid", "Joueur", "player", true, null),
             new("message", "Motif", "text", false, "Motif affiché au joueur")]),
        new("ban", "Bannir un joueur", "mdi-account-cancel-outline", RoleCode.Admin, true,
            [new("userid", "Joueur", "player", true, null),
             new("message", "Motif", "text", false, "Motif affiché au joueur")]),
        new("unban", "Lever un bannissement", "mdi-account-check-outline", RoleCode.Admin, false,
            [new("userid", "Identifiant du joueur", "text", true, "steam_76561198...")]),
        new("shutdown", "Arrêter avec un délai", "mdi-timer-off-outline", RoleCode.Admin, true,
            [new("waittime", "Délai (secondes)", "number", true, "60"),
             new("message", "Message", "text", false, "Message affiché avant l'arrêt")]),
        new("stop", "Arrêter immédiatement", "mdi-stop-circle-outline", RoleCode.Admin, true, []),
    ];

    public async Task ExecuteAsync(
        GameServerTarget target,
        string actionCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        // Le corps attendu par le jeu diffère d'une commande à l'autre ; les codes sont ceux
        // déclarés ci-dessus, déjà validés par le use case.
        object? body = actionCode switch
        {
            "announce" => new { message = Parameter(parameters, "message") },
            "save" => null,
            "kick" or "ban" => new { userid = Parameter(parameters, "userid"), message = Parameter(parameters, "message") },
            "unban" => new { userid = Parameter(parameters, "userid") },
            "shutdown" => new { waittime = int.Parse(Parameter(parameters, "waittime")), message = Parameter(parameters, "message") },
            "stop" => null,
            _ => throw new InvalidOperationException($"Action inconnue : {actionCode}."),
        };

        await PostAsync(target, actionCode, body, cancellationToken);
    }

    private static string Parameter(IReadOnlyDictionary<string, string> parameters, string name) =>
        parameters.TryGetValue(name, out var value) ? value : string.Empty;

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
        var infoTask = GetAsync(target, "info", cancellationToken);
        var settingsTask = GetAsync(target, "settings", cancellationToken);
        await Task.WhenAll(infoTask, settingsTask);

        using var info = await infoTask;
        using var settings = await settingsTask;
        var root = info?.RootElement;

        return new GameServerDetailsView(
            // Le nom du manifest fait foi, pour que la popup et la carte du widget concordent.
            target.ServerName,
            target.GameName,
            target.PictureUrl,
            root is null ? null : ReadString(root.Value, "version"),
            root is null ? null : ReadString(root.Value, "description"),
            root is null ? null : ReadString(root.Value, "worldguid"),
            // Clone() est nécessaire : le JsonDocument est libéré à la sortie de cette méthode.
            settings?.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()),
            // Rempli par le use case, qui seul connaît les droits de l'appelant.
            []);
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
            ParseStructures(gameData),
            // Palworld n'expose aucun journal serveur.
            [],
            unavailable);
    }

    // Le ping n'existe que dans /players, le reste que dans /game-data : les deux sources sont
    // rapprochées par userId. Les clés de game-data sont celles du jeu, en PascalCase pour les
    // acteurs et en minuscules pour les champs de compte — vérifiées contre l'adaptateur existant.
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
                var lightUserId = ReadString(player, "userId");
                var ping = ReadDouble(player, "ping");
                if (lightUserId is not null && ping is not null)
                {
                    pings[lightUserId] = (int)Math.Round(ping.Value);
                }
            }
        }

        // Un pal de compagnie est relié à son dresseur par l'identifiant d'instance de celui-ci.
        var companions = actors.EnumerateArray()
            .Where(actor => IsActor(actor, "OtomoPal"))
            .ToLookup(actor => ReadString(actor, "TrainerInstanceID") ?? string.Empty, StringComparer.Ordinal);

        var result = new List<GameServerLivePlayer>();
        foreach (var actor in actors.EnumerateArray())
        {
            if (!IsActor(actor, "Player"))
            {
                continue;
            }

            var userId = ReadString(actor, "userid");
            var positionX = ReadDouble(actor, "LocationX");
            var positionY = ReadDouble(actor, "LocationY");
            var (mapX, mapY) = ToMapPoint(positionX, positionY);
            var companion = companions[ReadString(actor, "InstanceID") ?? string.Empty].FirstOrDefault();

            result.Add(new GameServerLivePlayer(
                ReadString(actor, "NickName") ?? "?",
                userId,
                userId is not null && pings.TryGetValue(userId, out var ping) ? ping : null,
                ReadInt(actor, "level"),
                ReadInt(actor, "HP"),
                ReadInt(actor, "MaxHP"),
                ReadString(actor, "GuildID"),
                ReadString(actor, "GuildName"),
                mapX,
                mapY,
                positionX,
                positionY,
                companion.ValueKind == JsonValueKind.Undefined
                    ? null
                    : new GameServerLiveCompanion(
                        ReadString(companion, "NickName") ?? "?",
                        ReadInt(companion, "level"),
                        ReadInt(companion, "HP"),
                        ReadInt(companion, "MaxHP"))));
        }

        return result;
    }

    // Les bases sont matérialisées par la PalBox de la guilde.
    private static IReadOnlyList<GameServerLiveStructure> ParseStructures(JsonDocument? gameData)
    {
        if (gameData is null || !gameData.RootElement.TryGetProperty("ActorData", out var actors))
        {
            return [];
        }

        var result = new List<GameServerLiveStructure>();
        var index = 0;
        foreach (var actor in actors.EnumerateArray())
        {
            if (ReadString(actor, "Type") != "PalBox")
            {
                continue;
            }

            var positionX = ReadDouble(actor, "LocationX");
            var positionY = ReadDouble(actor, "LocationY");
            if (positionX is null || positionY is null)
            {
                continue;
            }

            result.Add(new GameServerLiveStructure(
                ReadString(actor, "InstanceID") ?? $"base-{index++}",
                ReadString(actor, "Name") ?? "Base",
                ReadString(actor, "GuildID"),
                ReadString(actor, "GuildName"),
                positionX.Value,
                positionY.Value,
                CreatureCount: 0));
        }

        return CountBasePals(actors, result);
    }

    // Le jeu ne dit pas à quelle base appartient un pal : il ne donne que sa guilde et sa
    // position. Chaque pal est donc rattaché à la base la plus proche de sa propre guilde, comme
    // le fait déjà le module Palworld.
    private static IReadOnlyList<GameServerLiveStructure> CountBasePals(
        JsonElement actors,
        List<GameServerLiveStructure> structures)
    {
        if (structures.Count == 0)
        {
            return structures;
        }

        var counts = new int[structures.Count];
        foreach (var actor in actors.EnumerateArray())
        {
            if (!IsActor(actor, "BaseCampPal"))
            {
                continue;
            }

            var palX = ReadDouble(actor, "LocationX");
            var palY = ReadDouble(actor, "LocationY");
            if (palX is null || palY is null)
            {
                continue;
            }

            var guildId = ReadString(actor, "GuildID");
            var closest = -1;
            var closestDistance = double.MaxValue;
            for (var index = 0; index < structures.Count; index++)
            {
                if (structures[index].GroupId != guildId)
                {
                    continue;
                }

                var deltaX = structures[index].PositionX - palX.Value;
                var deltaY = structures[index].PositionY - palY.Value;
                var distance = (deltaX * deltaX) + (deltaY * deltaY);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = index;
                }
            }

            if (closest >= 0)
            {
                counts[closest]++;
            }
        }

        return structures
            .Select((structure, index) => structure with { CreatureCount = counts[index] })
            .ToList();
    }

    private static bool IsActor(JsonElement actor, string unitType) =>
        ReadString(actor, "Type") == "Character" && ReadString(actor, "UnitType") == unitType;

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

    private async Task PostAsync(GameServerTarget target, string route, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"http://{target.Host}:{target.Port}/v1/api/{route}");
        Authenticate(request, target);
        if (body is not null)
        {
            // StringContent et non JsonContent : ce dernier laisse HttpClient envoyer le corps en
            // « chunked », que le serveur Palworld refuse par un 411 Length Required. Sérialiser
            // d'abord donne une longueur connue, donc un Content-Length.
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static void Authenticate(HttpRequestMessage request, GameServerTarget target)
    {
        var username = GameServerProtocolConfig.GetString(target.ProtocolConfig, "username");
        var password = GameServerProtocolConfig.GetString(target.ProtocolConfig, "password");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
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
