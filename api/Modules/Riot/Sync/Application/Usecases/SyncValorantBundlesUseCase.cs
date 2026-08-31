using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Application.Usecases;

public sealed class SyncValorantBundlesUseCase(
    IValorantBundleDataProvider bundleDataProvider,
    IValorantBundleSyncRepository bundleSyncRepository
)
{
    public async Task<ValorantSyncReport> Execute()
    {
        var currentByAssetId = (await bundleSyncRepository.FindAll())
            .ToDictionary(bundle => bundle.AssetId);

        var external = await bundleDataProvider.FetchAll();
        var externalAssetIds = external.Select(bundle => bundle.AssetId).ToHashSet();

        var created = 0;
        var updated = 0;
        var deleted = 0;

        foreach (var bundle in external)
        {
            if (!currentByAssetId.TryGetValue(bundle.AssetId, out var existing))
            {
                await bundleSyncRepository.Save(bundle);
                created++;
                continue;
            }

            if (existing.Name != bundle.Name || existing.BannerUrl != bundle.BannerUrl)
            {
                await bundleSyncRepository.Update(existing.Id, bundle);
                updated++;
            }
        }

        foreach (var current in currentByAssetId.Values.Where(bundle => !externalAssetIds.Contains(bundle.AssetId)))
        {
            await bundleSyncRepository.Delete(current.Id);
            deleted++;
        }

        return new ValorantSyncReport(created, updated, deleted);
    }
}
