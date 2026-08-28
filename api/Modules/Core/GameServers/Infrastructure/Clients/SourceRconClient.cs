using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

// Client Source RCON réutilisable : il ouvre une connexion, s'authentifie, puis enchaîne
// plusieurs commandes dessus. Les serveurs Ark émettent des paquets « Keep Alive » spontanés :
// les réponses sont donc appariées par identifiant, jamais prises pour la première qui arrive.
public sealed class SourceRconClient : IAsyncDisposable
{
    private const int AuthenticationRequest = 3;
    private const int CommandRequest = 2;
    private const int AuthenticationFailed = -1;

    private readonly TcpClient client = new();
    private NetworkStream? stream;
    private int nextId = 1;

    public async Task<bool> ConnectAsync(string host, int port, string password, CancellationToken cancellationToken)
    {
        await client.ConnectAsync(host, port, cancellationToken);
        stream = client.GetStream();

        var id = nextId++;
        await WritePacketAsync(id, AuthenticationRequest, password, cancellationToken);

        // L'échec d'authentification se signale par un identifiant à -1, pas par une erreur.
        var answer = await ReadPacketAsync(cancellationToken);
        while (answer is not null && answer.Id != id && answer.Id != AuthenticationFailed)
        {
            answer = await ReadPacketAsync(cancellationToken);
        }

        return answer is not null && answer.Id != AuthenticationFailed;
    }

    public async Task<string?> ExecuteAsync(string command, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            throw new InvalidOperationException("La connexion RCON n'est pas ouverte.");
        }

        var id = nextId++;
        await WritePacketAsync(id, CommandRequest, command, cancellationToken);

        var answer = await ReadPacketAsync(cancellationToken);
        while (answer is not null && answer.Id != id)
        {
            answer = await ReadPacketAsync(cancellationToken);
        }

        return answer?.Body.Trim();
    }

    private async Task WritePacketAsync(int id, int type, string body, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(body);
        var packet = new byte[4 + 4 + 4 + payload.Length + 2];
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), packet.Length - 4);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), id);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), type);
        payload.CopyTo(packet, 12);
        await stream!.WriteAsync(packet, cancellationToken);
    }

    private async Task<RconPacket?> ReadPacketAsync(CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[4];
        if (!await ReadExactlyAsync(lengthBytes, cancellationToken))
        {
            return null;
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
        if (length is < 10 or > 4_194_304)
        {
            return null;
        }

        var payload = new byte[length];
        if (!await ReadExactlyAsync(payload, cancellationToken))
        {
            return null;
        }

        return new RconPacket(
            BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(4, 4)),
            Encoding.UTF8.GetString(payload, 8, length - 10));
    }

    private async Task<bool> ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var received = await stream!.ReadAsync(buffer[read..], cancellationToken);
            if (received == 0)
            {
                return false;
            }

            read += received;
        }

        return true;
    }

    public ValueTask DisposeAsync()
    {
        stream?.Dispose();
        client.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed record RconPacket(int Id, int Type, string Body);
}
