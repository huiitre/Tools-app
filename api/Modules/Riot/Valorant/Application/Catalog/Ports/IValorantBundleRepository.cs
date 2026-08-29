using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;

public interface IValorantBundleRepository
{
    Task<List<ValorantBundleView>> FindAll();
    Task<ValorantBundleView?> FindById(long id);
    Task<ValorantBundleView?> FindByAssetId(Guid assetId);
}
