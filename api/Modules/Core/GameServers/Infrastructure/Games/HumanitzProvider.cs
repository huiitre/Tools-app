using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;
using Tools.Api.Modules.Core.GameServers.Infrastructure.Clients;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Games;

// Son RCON ne connaît que la commande « info », dont on ne tire que le nombre de joueurs : pas
// assez pour un dashboard.
public sealed class HumanitzProvider(HumanitzRconClient humanitzRconClient) : IGameServerProvider
{
    public string GameCode => "HUMANITZ";

    public Task<GameServerStatus> FetchStatusAsync(GameServerTarget target, CancellationToken cancellationToken)
    {
        return humanitzRconClient.FetchStatusAsync(target, cancellationToken);
    }
}
