using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantStoreOffer(
    ValorantSkinView Skin,
    int Cost
);