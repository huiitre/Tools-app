using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Audit des commandes RCON libres : qui a tapé quoi, sur quel serveur, et ce que le jeu a
// répondu. Aucune purge n'est prévue, seule la lecture est bornée.
public interface IGameServerRawCommandHistoryRepository
{
    Task InsertAsync(long gameServerId, long userId, string command, string? answer);

    Task<IReadOnlyList<GameServerRawCommandHistoryEntry>> FindRecentAsync(long gameServerId, int limit);
}
