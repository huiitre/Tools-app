using Tools.Api.Modules.GameServers.Application.Dto;

namespace Tools.Api.Modules.GameServers.Application.Ports;

public interface ISteamAppDetailsProvider
{
    Task<SteamAppDetailsLookup> FindAsync(int steamAppId);
}
