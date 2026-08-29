namespace Tools.Api.Modules.Riot.Sync.Application;

public sealed record ValorantWeaponSyncData(
    Guid AssetId,
    string Name,
    string? Category,
    Guid? DefaultSkinAssetId,
    string? DisplayIconUrl
);
