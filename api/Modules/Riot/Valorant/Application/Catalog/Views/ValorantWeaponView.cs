namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantWeaponView(
    long Id,
    Guid AssetId,
    string Name,
    string? Category,
    Guid? DefaultSkinAssetId,
    string? DisplayIconUrl
);
