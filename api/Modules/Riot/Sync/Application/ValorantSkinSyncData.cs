namespace Tools.Api.Modules.Riot.Sync.Application;

public sealed record ValorantSkinSyncData(
    Guid AssetId,
    string Name,
    string? IconUrl,
    Guid? TierUuid,
    Guid? ContentTierUuid,
    Guid? WeaponAssetId,
    List<ValorantSkinLevelSyncData> Levels,
    List<ValorantSkinChromaSyncData> Chromas
);
