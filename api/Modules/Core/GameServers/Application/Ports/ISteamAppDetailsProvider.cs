using Tools.Api.Modules.Core.GameServers.Application.Dto;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports;

public interface ISteamAppDetailsProvider
{
    Task<SteamAppDetailsLookup> FindAsync(int steamAppId);
}
