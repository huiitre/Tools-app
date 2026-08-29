namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantContentTierView(
    long Id,
    Guid AssetId,
    string Name,
    string DevName,
    int Rank,
    int JuiceValue,
    int JuiceCost,
    string? HighlightColor,
    string? DisplayIconUrl
);
