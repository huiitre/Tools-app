namespace Tools.Api.Modules.Riot.Sync.Application;

public sealed record ValorantSkinLevelSyncData(
    Guid AssetId,
    int LevelIndex,
    string Name,
    string? LevelItem,
    string? DisplayIconUrl,
    string? StreamedVideoUrl
);
