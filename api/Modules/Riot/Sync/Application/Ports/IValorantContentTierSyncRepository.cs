using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantContentTierSyncRepository
{
    Task<List<ValorantContentTierView>> FindAll();
    Task<long> Save(ValorantContentTierSyncData data);
    Task Update(long id, ValorantContentTierSyncData data);
    Task Delete(long id);
}
