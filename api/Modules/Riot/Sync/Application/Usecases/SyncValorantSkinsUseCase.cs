using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Application.Usecases;

public sealed class SyncValorantSkinsUseCase(
    IValorantSkinDataProvider skinDataProvider,
    IValorantSkinSyncRepository skinSyncRepository,
    IValorantSkinLevelSyncRepository levelSyncRepository,
    IValorantSkinChromaSyncRepository chromaSyncRepository
)
{
    public async Task<ValorantSyncReport> Execute(Dictionary<Guid, long> weaponAssetIdToDbId)
    {
        var external = await skinDataProvider.FetchAll();

        var currentByAssetId = (await skinSyncRepository.FindAll())
            .ToDictionary(skin => skin.AssetId);

        var externalAssetIds = external.Select(skin => skin.AssetId).ToHashSet();

        var skinAssetIdToDbId = new Dictionary<Guid, long>();
        var created = 0;
        var updated = 0;
        var deleted = 0;

        foreach (var skin in external)
        {
            long? weaponId = skin.WeaponAssetId is { } weaponAssetId
                && weaponAssetIdToDbId.TryGetValue(weaponAssetId, out var resolvedWeaponId)
                    ? resolvedWeaponId
                    : null;

            if (!currentByAssetId.TryGetValue(skin.AssetId, out var existing))
            {
                skinAssetIdToDbId[skin.AssetId] = await skinSyncRepository.Save(skin, weaponId);
                created++;
                continue;
            }

            skinAssetIdToDbId[skin.AssetId] = existing.Id;

            var changed = existing.Name != skin.Name
                || existing.IconUrl != skin.IconUrl
                || existing.TierUuid != skin.TierUuid
                || existing.WeaponId != weaponId;

            if (changed)
            {
                await skinSyncRepository.Update(existing.Id, skin, weaponId);
                updated++;
            }
        }

        foreach (var current in currentByAssetId.Values.Where(skin => !externalAssetIds.Contains(skin.AssetId)))
        {
            await skinSyncRepository.Delete(current.Id);
            deleted++;
        }

        await SyncLevels(external, skinAssetIdToDbId);
        await SyncChromas(external, skinAssetIdToDbId);

        return new ValorantSyncReport(created, updated, deleted);
    }

    // Niveaux et chromas sont purgés puis réinsérés : ils n'ont pas d'identité propre à préserver,
    // et la comparaison un à un coûterait plus que la réécriture.
    private async Task SyncLevels(
        List<ValorantSkinSyncData> external,
        Dictionary<Guid, long> skinAssetIdToDbId)
    {
        await levelSyncRepository.DeleteAll();

        foreach (var skin in external)
        {
            if (!skinAssetIdToDbId.TryGetValue(skin.AssetId, out var skinId))
            {
                continue;
            }

            foreach (var level in skin.Levels)
            {
                await levelSyncRepository.Save(skinId, level);
            }
        }
    }

    private async Task SyncChromas(
        List<ValorantSkinSyncData> external,
        Dictionary<Guid, long> skinAssetIdToDbId)
    {
        await chromaSyncRepository.DeleteAll();

        foreach (var skin in external)
        {
            if (!skinAssetIdToDbId.TryGetValue(skin.AssetId, out var skinId))
            {
                continue;
            }

            foreach (var chroma in skin.Chromas)
            {
                await chromaSyncRepository.Save(skinId, chroma);
            }
        }
    }
}
