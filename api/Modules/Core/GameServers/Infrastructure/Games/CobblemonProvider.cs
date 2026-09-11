using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// Minecraft moddé Cobblemon, interrogé en RCON. Toutes les commandes utilisées sont vanilla et
// ont été vérifiées sur le serveur le 11/09/2026 ; elles ne rendent que du texte, lu par des
// expressions régulières. Aucune ne donne la version, l'uptime ni le journal.
public sealed partial class CobblemonProvider : IGameServerProvider, IGameServerDashboard, IGameServerActions
{
    // Réponses par lesquelles le serveur refuse une commande, vérifiées le 11/09/2026.
    private static readonly string[] Rejections =
    [
        "No player was found", "That player does not exist", "Only players may", "Unknown or incomplete command",
        "Incorrect argument", "Invalid", "Nothing changed"
    ];
    // Le RCON n'a aucune commande qui liste les gamerules : celles de la 1.21.1 et les deux de
    // Cobblemon, vérifiées sur le serveur. Une gamerule inconnue d'une autre version est ignorée.
    private static readonly string[] GameRules =
    [
        "announceAdvancements", "blockExplosionDropDecay", "commandBlockOutput", "commandModificationBlockLimit",
        "disableElytraMovementCheck", "disableRaids", "doDaylightCycle", "doEntityDrops", "doFireTick",
        "doImmediateRespawn", "doInsomnia", "doLimitedCrafting", "doMobLoot", "doMobSpawning", "doPatrolSpawning",
        "doTileDrops", "doTraderSpawning", "doVinesSpread", "doWardenSpawning", "doWeatherCycle", "drowningDamage",
        "enderPearlsVanishOnDeath", "fallDamage", "fireDamage", "forgiveDeadPlayers", "freezeDamage",
        "globalSoundEvents", "keepInventory", "lavaSourceConversion", "logAdminCommands", "maxCommandChainLength",
        "maxCommandForkCount", "maxEntityCramming", "mobExplosionDropDecay", "mobGriefing", "naturalRegeneration",
        "playersNetherPortalCreativeDelay", "playersNetherPortalDefaultDelay", "playersSleepingPercentage",
        "projectilesCanBreakBlocks", "randomTickSpeed", "reducedDebugInfo", "sendCommandFeedback",
        "showDeathMessages", "snowAccumulationHeight", "spawnChunkRadius", "spawnRadius",
        "spectatorsGenerateChunks", "tntExplosionDropDecay", "universalAnger", "waterSourceConversion",
        "doPokemonSpawning", "doPokemonLoot"
    ];

    public string GameCode => "COBBLEMON";

    // Le redémarrage n'existe pas en RCON : « stop » arrête proprement le serveur, et c'est la
    // politique restart: unless-stopped du conteneur qui le relance. Sans elle, il resterait arrêté.
    public IReadOnlyList<GameServerActionDefinition> Actions { get; } =
    [
        new("announce", "Annoncer un message", "mdi-bullhorn-outline", RoleCode.Moderator, false,
            [new("message", "Message", "text", true, "Message affiché à tous les joueurs")]),
        new("kick", "Expulser un joueur", "mdi-account-remove-outline", RoleCode.Moderator, false,
            [new("playerId", "Joueur", "player", true, null), new("reason", "Raison", "text", false, "Facultative")]),
        new("ban", "Bannir un joueur", "mdi-account-cancel-outline", RoleCode.Admin, true,
            [new("playerId", "Joueur", "player", true, null), new("reason", "Raison", "text", false, "Facultative")]),
        new("restart", "Redémarrer le serveur", "mdi-restart", RoleCode.Admin, true, []),
    ];

    public async Task ExecuteAsync(
        GameServerTarget target,
        string actionCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        await using var client = await ConnectAsync(target, cancellationToken)
            ?? throw new InvalidOperationException("Connexion ou authentification RCON impossible.");

        var command = actionCode switch
        {
            "announce" => $"tellraw @a {AnnouncementComponent(Parameter(parameters, "message"))}",
            "kick" => $"kick {await ResolvePlayerAsync(client, Parameter(parameters, "playerId"), cancellationToken)} {Reason(parameters)}",
            "ban" => $"ban {await ResolvePlayerAsync(client, Parameter(parameters, "playerId"), cancellationToken)} {Reason(parameters)}",
            "restart" => "stop",
            _ => throw new InvalidOperationException($"Action inconnue : {actionCode}."),
        };

        // « stop » coupe la connexion avant de répondre : une absence de réponse n'est pas un refus.
        var answer = await client.ExecuteAsync(command.TrimEnd(), cancellationToken);
        if (answer is not null && Rejections.Any(rejection => answer.StartsWith(rejection, StringComparison.Ordinal)))
        {
            throw AppException.Validation("GAME_SERVER_ACTION_REJECTED", $"Le serveur a refusé la commande : {answer}");
        }
    }

    public async Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        try
        {
            await using var client = await ConnectAsync(target, cancellationToken);
            var answer = client is null ? null : await client.ExecuteAsync("list", cancellationToken);
            if (answer is null)
            {
                return GameServerStatus.Offline;
            }

            // Réponse inattendue d'un serveur qui a pourtant répondu : en ligne, compte inconnu.
            var players = ParsePlayers(answer);
            return players is null
                ? new GameServerStatus(true, null, null)
                : new GameServerStatus(true, players.Online, players.Max);
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
        await using var client = await ConnectAsync(target, cancellationToken);

        // Serveur injoignable : l'identité vient de game_servers et reste affichable.
        if (client is null)
        {
            return Details(target, seed: null, settings: null);
        }

        var settings = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        void Add(string key, object? value)
        {
            if (value is not null)
            {
                settings[key] = JsonSerializer.SerializeToElement(value);
            }
        }

        Add("difficulty", Capture(DifficultyPattern(), await client.ExecuteAsync("difficulty", cancellationToken)));
        Add("maxPlayers", ParsePlayers(await client.ExecuteAsync("list", cancellationToken))?.Max);
        Add("tickRate", ParseTick(await client.ExecuteAsync("tick query", cancellationToken))?.TargetRate);
        Add("worldBorder", ParseNumber(Capture(WorldBorderPattern(), await client.ExecuteAsync("worldborder get", cancellationToken))));
        Add("whitelistedPlayers", CountOf(WhitelistPattern(), await client.ExecuteAsync("whitelist list", cancellationToken)));
        Add("bannedPlayers", CountOf(BanlistPattern(), await client.ExecuteAsync("banlist", cancellationToken)));

        foreach (var rule in GameRules)
        {
            var value = Capture(GameRulePattern(), await client.ExecuteAsync($"gamerule {rule}", cancellationToken));
            Add(rule, value is null ? null : ParseRuleValue(value));
        }

        var seed = Capture(SeedPattern(), await client.ExecuteAsync("seed", cancellationToken));
        return Details(target, seed, settings.Count == 0 ? null : settings);
    }

    public async Task<GameServerLiveView> FetchLiveAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        await using var client = await ConnectAsync(target, cancellationToken);
        if (client is null)
        {
            return new GameServerLiveView(null, null, null, null, null, null, null, null, [], [], [], ["players"]);
        }

        var players = ParsePlayers(await client.ExecuteAsync("list uuids", cancellationToken));
        var day = ParseNumber(Capture(TimePattern(), await client.ExecuteAsync("time query day", cancellationToken)));
        var tick = ParseTick(await client.ExecuteAsync("tick query", cancellationToken));

        var livePlayers = new List<GameServerLivePlayer>();
        foreach (var player in players?.Entries ?? [])
        {
            livePlayers.Add(await FetchPlayerAsync(client, player, cancellationToken));
        }

        return new GameServerLiveView(
            PlayerCount: players?.Online,
            MaxPlayers: players?.Max,
            // Les ticks par seconde sont l'équivalent Minecraft des FPS serveur.
            Fps: tick?.TicksPerSecond,
            AverageFps: null,
            FrameTimeMs: tick?.MillisecondsPerTick,
            UptimeSeconds: null,
            InGameDay: day is null ? null : (int)day.Value,
            BaseCount: null,
            Players: livePlayers,
            Structures: [],
            Log: [],
            Unavailable: players is null ? ["players"] : []);
    }

    // Quatre lectures par joueur : au plus dix joueurs, rafraîchies toutes les 5 s.
    private static async Task<GameServerLivePlayer> FetchPlayerAsync(
        SourceRconClient client,
        PlayerEntry player,
        CancellationToken cancellationToken)
    {
        var position = ParsePosition(EntityData(await client.ExecuteAsync($"data get entity {player.Name} Pos", cancellationToken)));
        var dimension = EntityData(await client.ExecuteAsync($"data get entity {player.Name} Dimension", cancellationToken))?.Trim('"');
        var health = ParseNumber(EntityData(await client.ExecuteAsync($"data get entity {player.Name} Health", cancellationToken))?.TrimEnd('f'));
        var maxHealth = ParseNumber(Capture(
            MaxHealthPattern(),
            await client.ExecuteAsync($"attribute {player.Name} minecraft:generic.max_health get", cancellationToken)));
        var level = ParseNumber(EntityData(await client.ExecuteAsync($"data get entity {player.Name} XpLevel", cancellationToken)));

        return new GameServerLivePlayer(
            player.Name,
            player.Id,
            Ping: null,
            Level: level is null ? null : (int)level.Value,
            Health: health is null ? null : (int)Math.Round(health.Value, MidpointRounding.AwayFromZero),
            MaxHealth: maxHealth is null ? null : (int)Math.Round(maxHealth.Value, MidpointRounding.AwayFromZero),
            GroupId: null,
            GroupName: null,
            // Vue de dessus : X et Z. L'altitude Y est à part, une carte n'en a pas l'usage.
            MapX: position is null ? null : Math.Round(position.Value.X),
            MapY: position is null ? null : Math.Round(position.Value.Z),
            PositionX: position?.X,
            PositionY: position?.Z,
            Companion: null,
            World: dimension,
            Altitude: position is null ? null : Math.Round(position.Value.Y));
    }

    private static GameServerDetailsView Details(
        GameServerTarget target,
        string? seed,
        IReadOnlyDictionary<string, JsonElement>? settings) => new(
        target.ServerName,
        target.GameName,
        target.PictureUrl,
        // Le RCON vanilla n'a aucune commande qui rende la version ni le MOTD.
        Version: null,
        Description: null,
        WorldId: seed,
        Settings: settings,
        // Rempli par le use case, qui seul connaît les droits de l'appelant.
        Actions: []);

    // Message système : préfixe « [Serveur] » en or, sans le « [Rcon] » qu'ajouterait « say ».
    // Sérialisé en JSON pour que guillemets et accents ne cassent pas la commande.
    private static string AnnouncementComponent(string message) => JsonSerializer.Serialize(new object[]
    {
        "",
        new { text = "[Serveur] ", color = "gold", bold = true },
        new { text = message, color = "yellow" },
    });

    // Le front envoie l'UUID du joueur, que « kick » et « ban » refusent : il est traduit en pseudo
    // parmi les joueurs connectés. Tout le reste est refusé, sélecteurs comme « @a » compris.
    private static async Task<string> ResolvePlayerAsync(SourceRconClient client, string player, CancellationToken cancellationToken)
    {
        var entries = ParsePlayers(await client.ExecuteAsync("list uuids", cancellationToken))?.Entries ?? [];
        return entries
                   .FirstOrDefault(entry => string.Equals(entry.Id, player, StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(entry.Name, player, StringComparison.OrdinalIgnoreCase))
                   ?.Name
               ?? throw AppException.Validation(
                   "GAME_SERVER_PLAYER_NOT_CONNECTED",
                   $"Aucun joueur connecté ne correspond à « {player} ».");
    }

    // Une raison tient sur une ligne : un retour à la ligne ne doit pas prolonger la commande.
    private static string Reason(IReadOnlyDictionary<string, string> parameters) =>
        string.Join(' ', Parameter(parameters, "reason").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string Parameter(IReadOnlyDictionary<string, string> parameters, string name) =>
        parameters.TryGetValue(name, out var value) ? value : string.Empty;

    // Null si le serveur est injoignable ou refuse le mot de passe.
    private static async Task<SourceRconClient?> ConnectAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        var password = GameServerProtocolConfig.GetString(target.ProtocolConfig, "rconPassword");
        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var client = new SourceRconClient();
        try
        {
            if (await client.ConnectAsync(target.Host, target.Port, password, cancellationToken))
            {
                return client;
            }
        }
        catch (Exception exception) when (exception is SocketException or IOException)
        {
        }

        await client.DisposeAsync();
        return null;
    }

    // « There are 1 of a max of 10 players online: huiitre (e4b97b7a-…) ». Avec « list » seul, les
    // UUID sont absents. Un pseudo Minecraft ne contient ni virgule ni espace.
    private static PlayerList? ParsePlayers(string? answer)
    {
        var match = answer is null ? Match.Empty : PlayerListPattern().Match(answer);
        if (!match.Success)
        {
            return null;
        }

        var entries = match.Groups["names"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => PlayerEntryPattern().Match(entry))
            .Where(entry => entry.Success)
            .Select(entry => new PlayerEntry(
                entry.Groups["name"].Value,
                entry.Groups["id"].Success ? entry.Groups["id"].Value : null))
            .ToList();

        return new PlayerList(
            int.Parse(match.Groups["online"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["max"].Value, CultureInfo.InvariantCulture),
            entries);
    }

    // « Target tick rate: 20.0 per second. Average time per tick: 11.5ms ». Un tick plus long que
    // la cible fait baisser le nombre de ticks par seconde, jamais au-dessus de la cible.
    private static TickRate? ParseTick(string? answer)
    {
        var rate = ParseNumber(Capture(TargetTickRatePattern(), answer));
        var milliseconds = ParseNumber(Capture(AverageTickPattern(), answer));
        if (rate is null || milliseconds is null)
        {
            return null;
        }

        var ticksPerSecond = milliseconds.Value > 0 ? Math.Min(rate.Value, 1000 / milliseconds.Value) : rate.Value;
        return new TickRate(rate.Value, Math.Round(ticksPerSecond, 1), milliseconds.Value);
    }

    // « [1105.95d, 63.0d, -509.07d] » : X, Y (altitude), Z.
    private static (double X, double Y, double Z)? ParsePosition(string? data)
    {
        var values = data is null ? [] : CoordinatePattern().Matches(data).Select(match => ParseNumber(match.Value)).ToList();
        return values is [{ } x, { } y, { } z] ? (x, y, z) : null;
    }

    private static object ParseRuleValue(string value)
    {
        if (bool.TryParse(value, out var flag))
        {
            return flag;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : value;
    }

    // « There are no bans » vaut zéro, « There are 3 ban(s): … » vaut trois.
    private static int? CountOf(Regex pattern, string? answer)
    {
        if (answer is null)
        {
            return null;
        }

        if (answer.StartsWith("There are no ", StringComparison.Ordinal))
        {
            return 0;
        }

        var count = Capture(pattern, answer);
        return count is null ? null : int.Parse(count, CultureInfo.InvariantCulture);
    }

    // Valeur rendue par « data get entity » : « huiitre has the following entity data: 6 ».
    private static string? EntityData(string? answer) => Capture(EntityDataPattern(), answer);

    private static string? Capture(Regex pattern, string? answer)
    {
        var match = answer is null ? Match.Empty : pattern.Match(answer);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static double? ParseNumber(string? value) =>
        double.TryParse(value?.TrimEnd('d', 'f'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private sealed record PlayerList(int Online, int Max, IReadOnlyList<PlayerEntry> Entries);

    private sealed record PlayerEntry(string Name, string? Id);

    private sealed record TickRate(double TargetRate, double TicksPerSecond, double MillisecondsPerTick);

    [GeneratedRegex(@"There are (?<online>\d+) of a max of (?<max>\d+) players online:?(?<names>[^\r\n]*)")]
    private static partial Regex PlayerListPattern();

    [GeneratedRegex(@"^(?<name>\S+)(?:\s+\((?<id>[0-9a-fA-F-]+)\))?$")]
    private static partial Regex PlayerEntryPattern();

    [GeneratedRegex(@"has the following entity data: (?<value>.+)$")]
    private static partial Regex EntityDataPattern();

    [GeneratedRegex(@"-?\d+(?:\.\d+)?(?:[eE]-?\d+)?d")]
    private static partial Regex CoordinatePattern();

    [GeneratedRegex(@"Max Health for entity \S+ is (?<value>[\d.]+)")]
    private static partial Regex MaxHealthPattern();

    [GeneratedRegex(@"The time is (?<value>\d+)")]
    private static partial Regex TimePattern();

    [GeneratedRegex(@"Target tick rate: (?<value>[\d.]+)")]
    private static partial Regex TargetTickRatePattern();

    [GeneratedRegex(@"Average time per tick: (?<value>[\d.]+)ms")]
    private static partial Regex AverageTickPattern();

    [GeneratedRegex(@"The difficulty is (?<value>\S+)")]
    private static partial Regex DifficultyPattern();

    [GeneratedRegex(@"currently (?<value>[\d.]+) block")]
    private static partial Regex WorldBorderPattern();

    [GeneratedRegex(@"There are (?<value>\d+) whitelisted")]
    private static partial Regex WhitelistPattern();

    [GeneratedRegex(@"There are (?<value>\d+) ban")]
    private static partial Regex BanlistPattern();

    [GeneratedRegex(@"currently set to: (?<value>\S+)")]
    private static partial Regex GameRulePattern();

    [GeneratedRegex(@"Seed: \[(?<value>-?\d+)\]")]
    private static partial Regex SeedPattern();
}
