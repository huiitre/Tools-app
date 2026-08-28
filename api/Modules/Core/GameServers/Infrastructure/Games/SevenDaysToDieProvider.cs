using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// Comme Rust : A2S seul, aucun dashboard. Le serveur expose aussi un telnet, non utilisé.
public sealed class SevenDaysToDieProvider(SteamA2sClient steamA2sClient) : IGameServerProvider
{
    public string GameCode => "7DTD";

    public Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        return steamA2sClient.FetchStatusAsync(target, cancellationToken);
    }
}
