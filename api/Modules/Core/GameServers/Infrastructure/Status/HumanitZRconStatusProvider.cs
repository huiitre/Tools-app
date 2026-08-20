using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Tools.Api.Modules.Core.GameServers.Application.Dto;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Status;

// RCON HumanitZ : même framing TCP que le RCON Source standard, mais une auth non conforme au
// spec Valve, vérifiée en direct sur un serveur réel le 20/08/2026 (voir api/docs/GAME_SERVERS.md) :
// succès = deux paquets reçus (le premier type=0/body="None" à ignorer, le second type=2),
// request_id toujours à 0 côté serveur (jamais comparé), échec = aucun paquet, juste une
// fermeture TCP après un délai. Aucune commande "listplayers" : "info" renvoie un texte libre
// dont seule la ligne "<N> connected." est exploitée pour le nombre de joueurs.
public sealed partial class HumanitZRconStatusProvider : IGameServerStatusProvider
{
    private const int AuthenticationRequest = 3;
    private const int CommandRequest = 2;
    private const int AuthenticationResponse = 2;
    private const int CommandResponse = 0;

    public string ProtocolType => "HUMANITZ_RCON";

    public async Task<GameServerStatus> FetchAsync(GameServerPollTarget gameServer, CancellationToken cancellationToken)
    {
        try
        {
            var password = GameServerProtocolConfig.GetString(gameServer, "rconPassword");
            if (string.IsNullOrWhiteSpace(password))
            {
                return GameServerStatus.Offline;
            }

            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync(gameServer.Host, gameServer.Port, cancellationToken);
            await using var stream = client.GetStream();

            if (!await AuthenticateAsync(stream, password, cancellationToken))
            {
                return GameServerStatus.Offline;
            }

            await WritePacketAsync(stream, 2, CommandRequest, "info", cancellationToken);
            var answer = await ReadPacketAsync(stream, cancellationToken);
            if (answer is null || answer.Type != CommandResponse)
            {
                return GameServerStatus.Offline;
            }

            return new GameServerStatus(
                true,
                ParsePlayerCount(answer.Body),
                GameServerProtocolConfig.GetPositiveInt(gameServer, "maxPlayersOverride"));
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

    private static async Task<bool> AuthenticateAsync(System.Net.Sockets.NetworkStream stream, string password, CancellationToken cancellationToken)
    {
        await WritePacketAsync(stream, 1, AuthenticationRequest, password, cancellationToken);

        // Deux paquets arrivent sur un succès (le premier, type=0, est un accusé de réception à
        // ignorer) ; un mot de passe invalide ne renvoie jamais rien, la lecture retourne alors
        // null (connexion fermée) plutôt qu'un paquet d'échec explicite.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var packet = await ReadPacketAsync(stream, cancellationToken);
            if (packet is null)
            {
                return false;
            }

            if (packet.Type == AuthenticationResponse)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task WritePacketAsync(System.Net.Sockets.NetworkStream stream, int id, int type, string body, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(body);
        var packet = new byte[4 + 4 + 4 + payload.Length + 2];
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), packet.Length - 4);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), id);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), type);
        payload.CopyTo(packet, 12);
        await stream.WriteAsync(packet, cancellationToken);
    }

    private static async Task<RconPacket?> ReadPacketAsync(System.Net.Sockets.NetworkStream stream, CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[4];
        if (!await ReadExactlyAsync(stream, lengthBytes, cancellationToken))
        {
            return null;
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
        if (length is < 10 or > 16_384)
        {
            return null;
        }

        var payload = new byte[length];
        if (!await ReadExactlyAsync(stream, payload, cancellationToken))
        {
            return null;
        }

        var id = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, 4));
        var type = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(4, 4));
        var body = Encoding.UTF8.GetString(payload, 8, length - 10);
        return new RconPacket(id, type, body);
    }

    private static async Task<bool> ReadExactlyAsync(System.Net.Sockets.NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var received = await stream.ReadAsync(buffer[read..], cancellationToken);
            if (received == 0)
            {
                return false;
            }

            read += received;
        }

        return true;
    }

    // Seule donnée structurée exploitable de la réponse "info" : la ligne "<N> connected.".
    // Le reste (season/weather/AI/FPS) est du texte libre non parsé pour l'instant.
    private static int? ParsePlayerCount(string body)
    {
        var match = ConnectedLine().Match(body);
        return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : null;
    }

    [GeneratedRegex(@"(?m)^(\d+) connected\.$")]
    private static partial Regex ConnectedLine();

    private sealed record RconPacket(int Id, int Type, string Body);
}
