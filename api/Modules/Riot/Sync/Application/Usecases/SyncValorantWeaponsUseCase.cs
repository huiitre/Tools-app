using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Application.Usecases;

public sealed class SyncValorantWeaponsUseCase(
    IValorantWeaponDataProvider weaponDataProvider,
    IValorantWeaponSyncRepository weaponSyncRepository
)
{
    public async Task<ValorantWeaponSyncResult> Execute()
    {
        var external = await weaponDataProvider.FetchAll();

        var currentByAssetId = (await weaponSyncRepository.FindAll())
            .ToDictionary(weapon => weapon.AssetId);

        var externalAssetIds = external.Select(weapon => weapon.AssetId).ToHashSet();

        // Les skins se rattachent à leur arme par cette correspondance : elle se construit ici,
        // pendant qu'on connaît déjà les identifiants créés.
        var weaponAssetIdToDbId = new Dictionary<Guid, long>();
        var created = 0;
        var updated = 0;
        var deleted = 0;

        foreach (var weapon in external)
        {
            if (!currentByAssetId.TryGetValue(weapon.AssetId, out var existing))
            {
                weaponAssetIdToDbId[weapon.AssetId] = await weaponSyncRepository.Save(weapon);
                created++;
                continue;
            }

            weaponAssetIdToDbId[weapon.AssetId] = existing.Id;

            var changed = existing.Name != weapon.Name
                || existing.Category != weapon.Category
                || existing.DefaultSkinAssetId != weapon.DefaultSkinAssetId
                || existing.DisplayIconUrl != weapon.DisplayIconUrl;

            if (changed)
            {
                await weaponSyncRepository.Update(existing.Id, weapon);
                updated++;
            }
        }

        foreach (var current in currentByAssetId.Values.Where(weapon => !externalAssetIds.Contains(weapon.AssetId)))
        {
            await weaponSyncRepository.Delete(current.Id);
            deleted++;
        }

        return new ValorantWeaponSyncResult(
            new ValorantSyncReport(created, updated, deleted),
            weaponAssetIdToDbId);
    }
}
