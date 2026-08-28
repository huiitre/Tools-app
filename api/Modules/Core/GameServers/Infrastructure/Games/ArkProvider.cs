using System.Text.RegularExpressions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// Ark Survival Ascended, interrogé en RCON. Le protocole ne rend que du texte et le jeu n'expose
// ni FPS, ni uptime, ni position : le dashboard affichera « indisponible » pour tout cela. Les
// commandes vérifiées sur le serveur sont ListPlayers et GetGameLog ; GetChat, GetTimeOfDay,
// GetServerInfo et ListActiveMods répondent « Server received, But no response!! ».
public sealed partial class ArkProvider : IGameServerProvider, IGameServerDashboard, IGameServerActions
{
    private const string NoPlayers = "No Players Connected";

    public string GameCode => "ARK_SA";

    // Ark n'a ni « unban » ni arrêt différé par RCON : sa liste est plus courte que celle de
    // Palworld, et le front s'y adapte sans rien savoir du jeu.
    public IReadOnlyList<GameServerActionDefinition> Actions { get; } =
    [
        new("announce", "Annoncer un message", "mdi-bullhorn-outline", RoleCode.Moderator, false,
            [new("message", "Message", "text", true, "Message à diffuser")]),
        new("save", "Sauvegarder le monde", "mdi-content-save-outline", RoleCode.Moderator, false, []),
        new("kick", "Expulser un joueur", "mdi-account-remove-outline", RoleCode.Moderator, false,
            [new("playerId", "Joueur", "player", true, null)]),
        new("ban", "Bannir un joueur", "mdi-account-cancel-outline", RoleCode.Admin, true,
            [new("playerId", "Joueur", "player", true, null)]),
    ];

    public async Task ExecuteAsync(
        GameServerTarget target,
        string actionCode,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var command = actionCode switch
        {
            "announce" => $"Broadcast {Parameter(parameters, "message")}",
            "save" => "SaveWorld",
            "kick" => $"KickPlayer {Parameter(parameters, "playerId")}",
            "ban" => $"BanPlayer {Parameter(parameters, "playerId")}",
            _ => throw new InvalidOperationException($"Action inconnue : {actionCode}."),
        };

        var password = GameServerProtocolConfig.GetString(target.ProtocolConfig, "rconPassword");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Aucun mot de passe RCON n'est configuré pour ce serveur.");
        }

        await using var client = new SourceRconClient();
        if (!await client.ConnectAsync(target.Host, target.Port, password, cancellationToken))
        {
            throw new InvalidOperationException("L'authentification RCON a échoué.");
        }

        await client.ExecuteAsync(command, cancellationToken);
    }

    private static string Parameter(IReadOnlyDictionary<string, string> parameters, string name) =>
        parameters.TryGetValue(name, out var value) ? value : string.Empty;

    public async Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        try
        {
            var password = GameServerProtocolConfig.GetString(target.ProtocolConfig, "rconPassword");
            if (string.IsNullOrWhiteSpace(password))
            {
                return GameServerStatus.Offline;
            }

            await using var client = new SourceRconClient();
            if (!await client.ConnectAsync(target.Host, target.Port, password, cancellationToken))
            {
                return GameServerStatus.Offline;
            }

            // ListPlayers seulement : GetGameLog viderait le journal que le dashboard doit lire.
            var answer = await client.ExecuteAsync("ListPlayers", cancellationToken);
            if (answer is null)
            {
                return GameServerStatus.Offline;
            }

            return new GameServerStatus(
                true,
                ParsePlayers(answer).Count,
                GameServerProtocolConfig.GetPositiveInt(target.ProtocolConfig, "maxPlayersOverride"));
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

    public Task<GameServerDetailsView> FetchDetailsAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        // Aucune commande RCON ne rend le nom, la version ni la configuration : tout ce qui est
        // connu vient de game_servers.
        return Task.FromResult(new GameServerDetailsView(
            target.ServerName,
            target.GameName,
            target.PictureUrl,
            Version: null,
            Description: null,
            WorldId: null,
            // Le RCON n'expose aucune configuration serveur.
            Settings: null,
            // Rempli par le use case, qui seul connaît les droits de l'appelant.
            Actions: []));
    }

    public async Task<GameServerLiveView> FetchLiveAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        var password = GameServerProtocolConfig.GetString(target.ProtocolConfig, "rconPassword");
        if (string.IsNullOrWhiteSpace(password))
        {
            return Unavailable("players", "log");
        }

        await using var client = new SourceRconClient();
        if (!await client.ConnectAsync(target.Host, target.Port, password, cancellationToken))
        {
            return Unavailable("players", "log");
        }

        var players = await client.ExecuteAsync("ListPlayers", cancellationToken);
        var log = await client.ExecuteAsync("GetGameLog", cancellationToken);

        var parsedPlayers = ParsePlayers(players);
        var unavailable = new List<string>();
        if (players is null)
        {
            unavailable.Add("players");
        }

        return new GameServerLiveView(
            PlayerCount: players is null ? null : parsedPlayers.Count,
            // Le RCON n'expose pas la limite du serveur : elle ne peut venir que du manifest.
            MaxPlayers: GameServerProtocolConfig.GetPositiveInt(target.ProtocolConfig, "maxPlayersOverride"),
            Fps: null,
            AverageFps: null,
            FrameTimeMs: null,
            UptimeSeconds: null,
            InGameDay: null,
            BaseCount: null,
            Players: parsedPlayers,
            // Ark n'expose aucune construction par RCON.
            Structures: [],
            Log: ParseLog(log),
            Unavailable: unavailable);
    }

    // « 0. Huiitre, 000283c4caf548ab807c3e53d6afb458 » — le nom peut contenir des virgules,
    // seul le dernier segment est l'identifiant.
    private static IReadOnlyList<GameServerLivePlayer> ParsePlayers(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer) || answer.Contains(NoPlayers, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var players = new List<GameServerLivePlayer>();
        foreach (Match match in PlayerLine().Matches(answer))
        {
            var entry = match.Groups["entry"].Value.Trim();
            var separator = entry.LastIndexOf(',');
            var name = separator > 0 ? entry[..separator].Trim() : entry;
            var id = separator > 0 ? entry[(separator + 1)..].Trim() : null;

            players.Add(new GameServerLivePlayer(
                name,
                string.IsNullOrWhiteSpace(id) ? null : id,
                Ping: null,
                Level: null,
                Health: null,
                MaxHealth: null,
                GroupId: null,
                GroupName: null,
                MapX: null,
                MapY: null,
                PositionX: null,
                PositionY: null,
                Companion: null));
        }

        return players;
    }

    // Le journal est vidé par le serveur à chaque lecture : une réponse vide veut dire « rien de
    // neuf », pas « indisponible ».
    private static IReadOnlyList<string> ParseLog(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer) || answer.Contains("no response", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return answer
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static GameServerLiveView Unavailable(params string[] sections) => new(
        null, null, null, null, null, null, null, null, [], [], [], sections);

    [GeneratedRegex(@"(?m)^\s*\d+\.\s+(?<entry>.+?)\s*$")]
    private static partial Regex PlayerLine();
}
