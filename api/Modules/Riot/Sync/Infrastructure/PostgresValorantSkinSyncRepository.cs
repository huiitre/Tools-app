using Tools.Api.Modules.Riot.Sync.Application;
using Tools.Api.Modules.Riot.Common.Infrastructure;
using Tools.Api.Modules.Riot.Sync.Application.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Sync.Infrastructure;

// La synchronisation ne compare que les colonnes du skin : niveaux, chromas, rareté et drapeaux de
// possession ne l'intéressent pas, la vue rendue les laisse donc vides.
public sealed class PostgresValorantSkinSyncRepository(RiotDatabase database) : IValorantSkinSyncRepository
{
    private sealed record SkinRow(long Id, Guid AssetId, string Name, string? IconUrl, Guid? TierUuid, long? WeaponId);

    public async Task<List<ValorantSkinView>> FindAll()
    {
        var rows = await database.Query<SkinRow>(
            """
            SELECT id AS Id, asset_id AS AssetId, name AS Name, icon_url AS IconUrl,
                   tier_uuid AS TierUuid, weapon_id AS WeaponId
            FROM tools_riot.valorant_weapon_skins
            """);

        return rows
            .Select(row => new ValorantSkinView(
                row.Id, row.AssetId, row.Name, row.IconUrl, row.TierUuid,
                null, row.WeaponId, [], [], false, false, null, null))
            .ToList();
    }

    public Task<long> Save(ValorantSkinSyncData data, long? weaponId) =>
        database.ExecuteScalar<long>(
            """
            INSERT INTO tools_riot.valorant_weapon_skins
                (asset_id, name, icon_url, tier_uuid, content_tier_uuid, weapon_id)
            VALUES (@AssetId, @Name, @IconUrl, @TierUuid, @ContentTierUuid, @WeaponId)
            RETURNING id
            """,
            new { data.AssetId, data.Name, data.IconUrl, data.TierUuid, data.ContentTierUuid, WeaponId = weaponId });

    public Task Update(long id, ValorantSkinSyncData data, long? weaponId) =>
        database.Execute(
            """
            UPDATE tools_riot.valorant_weapon_skins
            SET name = @Name, icon_url = @IconUrl, tier_uuid = @TierUuid,
                content_tier_uuid = @ContentTierUuid, weapon_id = @WeaponId
            WHERE id = @Id
            """,
            new { Id = id, data.Name, data.IconUrl, data.TierUuid, data.ContentTierUuid, WeaponId = weaponId });

    public Task Delete(long id) =>
        database.Execute("DELETE FROM tools_riot.valorant_weapon_skins WHERE id = @Id", new { Id = id });
}
