using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;

namespace Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

public interface ISteamAppDetailsProvider
{
    Task<SteamAppDetailsLookup> FindAsync(int steamAppId);
}
