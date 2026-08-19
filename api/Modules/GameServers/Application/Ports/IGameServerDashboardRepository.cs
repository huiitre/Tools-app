using Tools.Api.Modules.GameServers.Application.Dto;

namespace Tools.Api.Modules.GameServers.Application.Ports;

// Lecture dédiée au dashboard : elle ne projette jamais les credentials techniques.
public interface IGameServerDashboardRepository
{
    Task<IReadOnlyList<GameServerDashboardView>> FindVisibleForDashboardAsync();
}
