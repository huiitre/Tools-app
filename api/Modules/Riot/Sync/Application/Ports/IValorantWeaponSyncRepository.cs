using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantWeaponSyncRepository
{
    Task<List<ValorantWeaponView>> FindAll();
    Task<long> Save(ValorantWeaponSyncData data);
    Task Update(long id, ValorantWeaponSyncData data);
    Task Delete(long id);
}
