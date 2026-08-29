namespace Tools.Api.Modules.Riot.Sync.Application.Ports;

public interface IValorantWeaponDataProvider
{
    Task<List<ValorantWeaponSyncData>> FetchAll();
}
