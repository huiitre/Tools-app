using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Infrastructure.Status;

// Source RCON ASA : listplayers donne le nombre connecté ; maxPlayers n'est pas exposé par RCON
// et vient uniquement de protocolConfig.maxPlayersOverride.
public sealed partial class SourceRconStatusProvider : IGameServerStatusProvider
{
    private const int AuthenticationRequest = 3;
    private const int CommandRequest = 2;
    private const int AuthenticationResponse = 2;
    private const int CommandResponse = 0;

    public string ProtocolType => "SOURCE_RCON";

    public async Task<GameServerStatus> FetchAsync(GameServerPollTarget gameServer, CancellationToken cancellationToken)
    {
        try
        {
            var password = GameServerProtocolConfig.GetString(gameServer, "rconPassword");
            if (string.IsNullOrWhiteSpace(password))
            {
                return GameServerStatus.Offline;
            }

            using var client = new TcpClient();
            await client.ConnectAsync(gameServer.Host, gameServer.Port, cancellationToken);
            await using var stream = client.GetStream();

            const int authId = 1;
            await WritePacketAsync(stream, authId, AuthenticationRequest, password, cancellationToken);
            var authentication = await ReadPacketAsync(stream, cancellationToken);
            if (authentication is null || authentication.Id != authId || authentication.Type != AuthenticationResponse)
            {
                return GameServerStatus.Offline;
            }

            const int commandId = 2;
            await WritePacketAsync(stream, commandId, CommandRequest, "listplayers", cancellationToken);
            var answer = await ReadPacketAsync(stream, cancellationToken);
            if (answer is null || answer.Id != commandId || answer.Type != CommandResponse)
            {
                return GameServerStatus.Offline;
            }

            var numberOfPlayers = CountPlayers(answer.Body);
            return new GameServerStatus(
                true,
                numberOfPlayers,
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

    private static async Task WritePacketAsync(NetworkStream stream, int id, int type, string body, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(body);
        var packet = new byte[4 + 4 + 4 + payload.Length + 2];
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), packet.Length - 4);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), id);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), type);
        payload.CopyTo(packet, 12);
        await stream.WriteAsync(packet, cancellationToken);
    }

    private static async Task<RconPacket?> ReadPacketAsync(NetworkStream stream, CancellationToken cancellationToken)
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

    private static async Task<bool> ReadExactlyAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken cancellationToken)
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

    private static int CountPlayers(string answer)
    {
        if (answer.Contains("No Players Connected", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return PlayerLine().Matches(answer).Count;
    }

    [GeneratedRegex(@"(?m)^\s*\d+\.\s+.+$")]
    private static partial Regex PlayerLine();

    private sealed record RconPacket(int Id, int Type, string Body);
}
