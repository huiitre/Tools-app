using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class PostgresValorantBundleSyncRepository(RiotDatabase database) : IValorantBundleSyncRepository
{
    public Task<List<ValorantBundleView>> FindAll() =>
        database.Query<ValorantBundleView>(
            """
            SELECT id AS Id, asset_id AS AssetId, name AS Name, banner_url AS BannerUrl
            FROM tools_riot.valorant_bundles
            """);

    public Task<long> Save(ValorantBundleSyncData data) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_bundles (asset_id, name, banner_url)
            VALUES (@AssetId, @Name, @BannerUrl)
            RETURNING id
            """,
            data);

    public Task Update(long id, ValorantBundleSyncData data) =>
        database.Execute(
            """
            UPDATE tools_riot.valorant_bundles
            SET name = @Name, banner_url = @BannerUrl
            WHERE id = @Id
            """,
            new { Id = id, data.Name, data.BannerUrl });

    public Task Delete(long id) =>
        database.Execute("DELETE FROM tools_riot.valorant_bundles WHERE id = @Id", new { Id = id });
}
