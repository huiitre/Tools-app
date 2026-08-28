using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Polling;

// Port isolé du poll : le flux sync ne lit ni n'écrit jamais ces colonnes de statut.
public interface IGameServerPollingRepository
{
    // Cible technique privée : protocolConfig porte des credentials et ne sort jamais de l'API.
    Task<IReadOnlyList<GameServerTarget>> FindAllForPollingAsync();

    // Écriture strictement réservée au poll : elle ne modifie jamais la configuration syncée.
    Task UpdateStatusAsync(long id, GameServerStatus status);
}
