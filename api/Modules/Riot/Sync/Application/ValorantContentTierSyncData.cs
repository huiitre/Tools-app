namespace Tools.Api.Modules.Riot.Sync.Application;

public sealed record ValorantContentTierSyncData(
    Guid AssetId,
    string Name,
    string DevName,
    int Rank,
    int JuiceValue,
    int JuiceCost,
    string? HighlightColor,
    string? DisplayIconUrl
);
