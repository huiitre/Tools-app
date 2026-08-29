using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.IntegrationTests.Fakes;

// Catalogue Valorant en mémoire : un seul pack, pour éprouver le câblage HTTP du module sans
// PostgreSQL.
public sealed class InMemoryValorantCatalogRepository : IValorantBundleRepository
{
    public static readonly Guid ExistingBundleAssetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const long ExistingBundleId = 1;

    private readonly List<ValorantBundleView> bundles =
    [
        new(ExistingBundleId, ExistingBundleAssetId, "Pack de test", null)
    ];

    public Task<List<ValorantBundleView>> FindAll() => Task.FromResult(bundles);

    public Task<ValorantBundleView?> FindById(long id) =>
        Task.FromResult(bundles.FirstOrDefault(bundle => bundle.Id == id));

    public Task<ValorantBundleView?> FindByAssetId(Guid assetId) =>
        Task.FromResult(bundles.FirstOrDefault(bundle => bundle.AssetId == assetId));
}
