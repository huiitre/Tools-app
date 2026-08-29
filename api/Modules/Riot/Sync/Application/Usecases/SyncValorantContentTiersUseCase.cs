using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Sync.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Application.Usecases;

public sealed class SyncValorantContentTiersUseCase(
    UseCaseAuthorizer authorizer,
    IValorantContentTierDataProvider contentTierDataProvider,
    IValorantContentTierSyncRepository contentTierSyncRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Tech;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<ValorantSyncReport> Execute()
    {
        var currentByAssetId = (await contentTierSyncRepository.FindAll())
            .ToDictionary(tier => tier.AssetId);

        var external = await contentTierDataProvider.FetchAll();
        var externalAssetIds = external.Select(tier => tier.AssetId).ToHashSet();

        var created = 0;
        var updated = 0;
        var deleted = 0;

        foreach (var tier in external)
        {
            if (!currentByAssetId.TryGetValue(tier.AssetId, out var existing))
            {
                await contentTierSyncRepository.Save(tier);
                created++;
                continue;
            }

            var changed = existing.Name != tier.Name
                || existing.DevName != tier.DevName
                || existing.Rank != tier.Rank
                || existing.JuiceValue != tier.JuiceValue
                || existing.JuiceCost != tier.JuiceCost
                || existing.HighlightColor != tier.HighlightColor
                || existing.DisplayIconUrl != tier.DisplayIconUrl;

            if (changed)
            {
                await contentTierSyncRepository.Update(existing.Id, tier);
                updated++;
            }
        }

        foreach (var current in currentByAssetId.Values.Where(tier => !externalAssetIds.Contains(tier.AssetId)))
        {
            await contentTierSyncRepository.Delete(current.Id);
            deleted++;
        }

        return new ValorantSyncReport(created, updated, deleted);
    }
}
