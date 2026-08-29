namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantStoreBundle(
    string AssetId,
    string Name,
    string BannerUrl,
    List<ValorantStoreOffer> Items,
    int TotalBaseCost,
    int TotalDiscountedCost,
    int DiscountPercent,
    long RemainingSeconds
);