namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantBundleDataProvider
{
    Task<List<ValorantBundleSyncData>> FetchAll();
}
