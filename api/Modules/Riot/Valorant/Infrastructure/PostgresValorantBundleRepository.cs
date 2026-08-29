using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Valorant.Infrastructure;

public sealed class PostgresValorantBundleRepository(RiotDatabase database) : IValorantBundleRepository
{
    private const string Select = """
        SELECT id AS Id, asset_id AS AssetId, name AS Name, banner_url AS BannerUrl
        FROM tools_riot.valorant_bundles
        """;

    public Task<List<ValorantBundleView>> FindAll() =>
        database.Query<ValorantBundleView>($"{Select} ORDER BY name");

    public Task<ValorantBundleView?> FindById(long id) =>
        database.QueryFirstOrDefault<ValorantBundleView>($"{Select} WHERE id = @Id", new { Id = id });

    public Task<ValorantBundleView?> FindByAssetId(Guid assetId) =>
        database.QueryFirstOrDefault<ValorantBundleView>($"{Select} WHERE asset_id = @AssetId", new { AssetId = assetId });
}
