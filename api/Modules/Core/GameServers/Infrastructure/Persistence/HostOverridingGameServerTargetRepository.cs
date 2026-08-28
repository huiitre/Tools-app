using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Persistence;

// Pendant de HostOverridingGameServerPollingRepository pour l'interrogation en direct : sans lui,
// le dashboard local viserait les IP docker du NAS au lieu du tunnel.
public sealed class HostOverridingGameServerTargetRepository(
    IGameServerTargetRepository inner,
    string host) : IGameServerTargetRepository
{
    public async Task<GameServerTarget?> FindBySlugAsync(string slug)
    {
        var target = await inner.FindBySlugAsync(slug);
        return target is null ? null : target with { Host = host };
    }
}
