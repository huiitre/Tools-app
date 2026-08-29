namespace Tools.Api.Modules.Riot.Sync.Application;

public sealed record ValorantBundleSyncData(
    Guid AssetId,
    string Name,
    string? BannerUrl
);
