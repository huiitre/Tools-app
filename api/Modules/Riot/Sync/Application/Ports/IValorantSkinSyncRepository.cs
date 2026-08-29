using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantSkinSyncRepository
{
    Task<List<ValorantSkinView>> FindAll();
    Task<long> Save(ValorantSkinSyncData data, long? weaponId);
    Task Update(long id, ValorantSkinSyncData data, long? weaponId);
    Task Delete(long id);
}
