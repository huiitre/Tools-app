using System.Globalization;
using System.Text.RegularExpressions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// Minecraft moddé Cobblemon, interrogé en RCON. « list » donne à la fois les joueurs connectés,
// leurs noms et le maximum : aucun maxPlayersOverride n'est nécessaire, et c'est tout ce que le
// dashboard affiche pour l'instant.
public sealed partial class CobblemonProvider : IGameServerProvider, IGameServerDashboard
{
    public string GameCode => "COBBLEMON";

    public async Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        try
        {
            var answer = await ListAsync(target, cancellationToken);
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

    public Task<GameServerDetailsView> FetchDetailsAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        // Le RCON vanilla n'a aucune commande qui rende la version ou la configuration : tout ce
        // qui est connu vient de game_servers.
        return Task.FromResult(new GameServerDetailsView(
            target.ServerName,
            target.GameName,
            target.PictureUrl,
            Version: null,
            Description: null,
            WorldId: null,
            Settings: null,
            // Rempli par le use case, qui seul connaît les droits de l'appelant.
            Actions: []));
    }

    public async Task<GameServerLiveView> FetchLiveAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        var answer = await ListAsync(target, cancellationToken);
        var players = answer is null ? null : ParsePlayers(answer);
        if (players is null)
        {
            return new GameServerLiveView(null, null, null, null, null, null, null, null, [], [], [], ["players"]);
        }

        return new GameServerLiveView(
            PlayerCount: players.Online,
            MaxPlayers: players.Max,
            Fps: null,
            AverageFps: null,
            FrameTimeMs: null,
            UptimeSeconds: null,
            InGameDay: null,
            BaseCount: null,
            // Le RCON ne donne que le nom : ni identifiant, ni position, ni santé.
            Players: players.Names
                .Select(name => new GameServerLivePlayer(
                    name, null, null, null, null, null, null, null, null, null, null, null, null))
                .ToList(),
            Structures: [],
            Log: [],
            Unavailable: []);
    }

    // Null si le serveur n'a pas pu être joint ou n'a rien répondu.
    private static async Task<string?> ListAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        var password = GameServerProtocolConfig.GetString(target.ProtocolConfig, "rconPassword");
        if (string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        await using var client = new SourceRconClient();
        if (!await client.ConnectAsync(target.Host, target.Port, password, cancellationToken))
        {
            return null;
        }

        return await client.ExecuteAsync("list", cancellationToken);
    }

    // « There are 2 of a max of 20 players online: Huiitre, Alex ». Un pseudo Minecraft ne peut
    // pas contenir de virgule : le découpage est sûr.
    private static PlayerList? ParsePlayers(string answer)
    {
        var match = PlayerCount().Match(answer);
        if (!match.Success)
        {
            return null;
        }

        return new PlayerList(
            int.Parse(match.Groups["online"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["max"].Value, CultureInfo.InvariantCulture),
            match.Groups["names"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private sealed record PlayerList(int Online, int Max, IReadOnlyList<string> Names);

    [GeneratedRegex(@"There are (?<online>\d+) of a max of (?<max>\d+) players online:?(?<names>[^\r\n]*)")]
    private static partial Regex PlayerCount();
}
