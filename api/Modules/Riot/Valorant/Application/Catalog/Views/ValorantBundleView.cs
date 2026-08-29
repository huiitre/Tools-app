namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantBundleView(
    long Id,
    Guid AssetId,
    string Name,
    string? BannerUrl
);
