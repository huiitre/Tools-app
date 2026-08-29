using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;

public interface IValorantWeaponRepository
{
    Task<List<ValorantWeaponView>> FindAll();
    Task<ValorantWeaponView?> FindById(long id);
}
