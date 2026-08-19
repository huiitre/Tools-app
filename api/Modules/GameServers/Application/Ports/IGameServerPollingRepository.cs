using Tools.Api.Modules.GameServers.Application.Dto;

namespace Tools.Api.Modules.GameServers.Application.Ports;

// Port isolé du poll : le flux sync ne lit ni n'écrit jamais ces colonnes de statut.
public interface IGameServerPollingRepository
{
    // Cible technique privée. protocolConfig peut contenir des credentials et ne sort pas de l'API.
    Task<IReadOnlyList<GameServerPollTarget>> FindAllForPollingAsync();

    // Écriture strictement réservée au poll : elle ne modifie jamais la configuration syncée.
    Task UpdateStatusAsync(long id, GameServerStatus status);
}
