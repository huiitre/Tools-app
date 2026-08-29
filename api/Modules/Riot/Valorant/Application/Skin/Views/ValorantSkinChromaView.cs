namespace Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

public sealed record ValorantSkinChromaView(
    Guid AssetId,
    int ChromaIndex,
    string Name,
    string? DisplayIconUrl,
    string? FullRenderUrl,
    string? SwatchUrl,
    string? StreamedVideoUrl
);
