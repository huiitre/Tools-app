using System.Net;
using System.Net.Sockets;
using System.Text;
using Tools.Api.Modules.Core.GameServers.Application.Dto;
using Tools.Api.Modules.Core.GameServers.Application.Ports;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Status;

// Client UDP A2S_INFO standard Source. Il gère aussi le challenge A2S, fréquent sur les serveurs
// Steam, sans connaître le jeu interrogé (Rust n'est donc pas codé en dur).
public sealed class SteamA2sStatusProvider : IGameServerStatusProvider
{
    private static readonly byte[] InfoQuery = [
        0xFF, 0xFF, 0xFF, 0xFF, 0x54,
        .. Encoding.ASCII.GetBytes("Source Engine Query"), 0x00
    ];

    public string ProtocolType => "STEAM_A2S";

    public async Task<GameServerStatus> FetchAsync(GameServerPollTarget gameServer, CancellationToken cancellationToken)
    {
        try
        {
            using var udpClient = new UdpClient();
            var addresses = await Dns.GetHostAddressesAsync(gameServer.Host, cancellationToken);
            var address = addresses.FirstOrDefault()
                ?? throw new SocketException((int)SocketError.HostNotFound);
            var endpoint = new IPEndPoint(address, gameServer.Port);

            await udpClient.SendAsync(InfoQuery, endpoint, cancellationToken);
            var response = (await udpClient.ReceiveAsync(cancellationToken)).Buffer;

            if (IsChallenge(response))
            {
                var challengedQuery = new byte[InfoQuery.Length + 4];
                Buffer.BlockCopy(InfoQuery, 0, challengedQuery, 0, InfoQuery.Length);
                Buffer.BlockCopy(response, 5, challengedQuery, InfoQuery.Length, 4);
                await udpClient.SendAsync(challengedQuery, endpoint, cancellationToken);
                response = (await udpClient.ReceiveAsync(cancellationToken)).Buffer;
            }

            return ParseInfoResponse(response);
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

    private static bool IsChallenge(byte[] response) => response.Length >= 9
        && HasHeader(response)
        && response[4] == 0x41;

    private static GameServerStatus ParseInfoResponse(byte[] response)
    {
        // header (4), type I (1), protocol (1), then name/map/folder/game strings and app id.
        if (response.Length < 6 || !HasHeader(response) || response[4] != 0x49)
        {
            return GameServerStatus.Offline;
        }

        var offset = 6;
        for (var index = 0; index < 4; index++)
        {
            offset = SkipNullTerminatedString(response, offset);
            if (offset < 0)
            {
                return GameServerStatus.Offline;
            }
        }

        // AppID (ushort), then players and max players (byte each).
        if (offset + 4 > response.Length)
        {
            return GameServerStatus.Offline;
        }

        var numPlayers = response[offset + 2];
        var maxPlayers = response[offset + 3];
        return new GameServerStatus(true, numPlayers, maxPlayers);
    }

    private static int SkipNullTerminatedString(byte[] buffer, int offset)
    {
        var end = Array.IndexOf(buffer, (byte)0, offset);
        return end < 0 ? -1 : end + 1;
    }

    private static bool HasHeader(byte[] response) => response[0] == 0xFF
        && response[1] == 0xFF
        && response[2] == 0xFF
        && response[3] == 0xFF;
}
