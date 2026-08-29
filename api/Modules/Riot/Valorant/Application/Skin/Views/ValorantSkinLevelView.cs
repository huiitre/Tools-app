namespace Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

public sealed record ValorantSkinLevelView(
    Guid AssetId,
    int LevelIndex,
    string Name,
    string? LevelItem,
    string? DisplayIconUrl,
    string? StreamedVideoUrl
);
