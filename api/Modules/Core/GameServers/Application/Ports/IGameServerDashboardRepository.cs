using Tools.Api.Modules.Core.GameServers.Application.Dto;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports;

// Lecture dédiée au dashboard : elle ne projette jamais les credentials techniques.
public interface IGameServerDashboardRepository
{
    Task<IReadOnlyList<GameServerDashboardView>> FindVisibleForDashboardAsync();
}
