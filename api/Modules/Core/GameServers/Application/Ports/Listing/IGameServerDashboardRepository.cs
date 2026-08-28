using Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Listing;

// Lecture dédiée au dashboard : elle ne projette jamais les credentials techniques.
public interface IGameServerDashboardRepository
{
    Task<IReadOnlyList<GameServerListRow>> FindVisibleForDashboardAsync();
}
