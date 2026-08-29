namespace Tools.Api.Modules.Riot.Sync.Application;

public sealed record ValorantSkinChromaSyncData(
    Guid AssetId,
    int ChromaIndex,
    string Name,
    string? DisplayIconUrl,
    string? FullRenderUrl,
    string? SwatchUrl,
    string? StreamedVideoUrl
);
