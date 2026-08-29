using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

public sealed class PostgresValorantWeaponSyncRepository(RiotDatabase database) : IValorantWeaponSyncRepository
{
    public Task<List<ValorantWeaponView>> FindAll() =>
        database.Query<ValorantWeaponView>(
            """
            SELECT id AS Id, asset_id AS AssetId, name AS Name, category AS Category,
                   default_skin_asset_id AS DefaultSkinAssetId, display_icon_url AS DisplayIconUrl
            FROM tools_riot.valorant_weapons
            """);

    public Task<long> Save(ValorantWeaponSyncData data) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_weapons (asset_id, name, category, default_skin_asset_id, display_icon_url)
            VALUES (@AssetId, @Name, @Category, @DefaultSkinAssetId, @DisplayIconUrl)
            RETURNING id
            """,
            data);

    public Task Update(long id, ValorantWeaponSyncData data) =>
        database.Execute(
            """
            UPDATE tools_riot.valorant_weapons
            SET name = @Name, category = @Category, default_skin_asset_id = @DefaultSkinAssetId,
                display_icon_url = @DisplayIconUrl
            WHERE id = @Id
            """,
            new { Id = id, data.Name, data.Category, data.DefaultSkinAssetId, data.DisplayIconUrl });

    public Task Delete(long id) =>
        database.Execute("DELETE FROM tools_riot.valorant_weapons WHERE id = @Id", new { Id = id });
}
