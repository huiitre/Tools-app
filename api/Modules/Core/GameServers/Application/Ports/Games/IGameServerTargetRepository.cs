using Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Games;

// Lecture d'un serveur unique pour l'interroger en direct. Distincte du port de poll, qui ne sait
// que ramener la totalité des cibles.
public interface IGameServerTargetRepository
{
    Task<GameServerTarget?> FindBySlugAsync(string slug);
}
