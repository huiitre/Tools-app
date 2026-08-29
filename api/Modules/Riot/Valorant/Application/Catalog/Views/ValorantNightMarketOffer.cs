using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantNightMarketOffer(
    string OfferId,
    ValorantSkinView Skin,
    int OriginalCost,
    int DiscountedCost,
    int DiscountPercent,
    bool IsSeen
);