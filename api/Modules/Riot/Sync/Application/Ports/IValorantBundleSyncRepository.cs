using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantBundleSyncRepository
{
    Task<List<ValorantBundleView>> FindAll();
    Task<long> Save(ValorantBundleSyncData data);
    Task Update(long id, ValorantBundleSyncData data);
    Task Delete(long id);
}
