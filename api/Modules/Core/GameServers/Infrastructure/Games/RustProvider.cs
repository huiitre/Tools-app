using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// Rust n'est interrogeable qu'en A2S, qui ne donne que le nombre de joueurs : pas de dashboard,
// donc pas d'IGameServerDashboard.
public sealed class RustProvider(SteamA2sClient steamA2sClient) : IGameServerProvider
{
    public string GameCode => "RUST";

    public Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        return steamA2sClient.FetchStatusAsync(target, cancellationToken);
    }
}
