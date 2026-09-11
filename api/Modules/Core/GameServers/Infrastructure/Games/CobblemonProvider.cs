using System.Globalization;
using System.Text.RegularExpressions;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// Minecraft moddé Cobblemon, interrogé en RCON. « list » donne à la fois les joueurs connectés
// et le maximum : aucun maxPlayersOverride n'est nécessaire.
public sealed partial class CobblemonProvider : IGameServerProvider
{
    public string GameCode => "COBBLEMON";

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

            var answer = await client.ExecuteAsync("list", cancellationToken);
            if (answer is null)
            {
                return GameServerStatus.Offline;
            }

            // Réponse inattendue d'un serveur qui a pourtant répondu : en ligne, compte inconnu.
            var match = PlayerCount().Match(answer);
            return match.Success
                ? new GameServerStatus(
                    true,
                    int.Parse(match.Groups["online"].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["max"].Value, CultureInfo.InvariantCulture))
                : new GameServerStatus(true, null, null);
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

    [GeneratedRegex(@"There are (?<online>\d+) of a max of (?<max>\d+) players online")]
    private static partial Regex PlayerCount();
}
