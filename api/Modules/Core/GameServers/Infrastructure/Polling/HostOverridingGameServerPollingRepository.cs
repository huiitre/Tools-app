using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Polling;

namespace Tools.Api.Modules.Core.GameServers.Infrastructure.Polling;

// Le manifest porte les IP docker du NAS, injoignables depuis un poste de dev ; le tunnel SSH de
// dev-console les ramène sur 127.0.0.1 en conservant les ports. Ce décorateur y redirige les
// cibles sans toucher ni au manifest ni à la base — il n'est enregistré que si l'hôte est
// configuré, donc la production ne le traverse jamais.
public sealed class HostOverridingGameServerPollingRepository(
    IGameServerPollingRepository inner,
    string host) : IGameServerPollingRepository
{
    public async Task<IReadOnlyList<GameServerTarget>> FindAllForPollingAsync()
    {
        var targets = await inner.FindAllForPollingAsync();
        return targets.Select(target => target with { Host = host }).ToList();
    }

    public Task UpdateStatusAsync(long id, GameServerStatus status)
    {
        return inner.UpdateStatusAsync(id, status);
    }
}
