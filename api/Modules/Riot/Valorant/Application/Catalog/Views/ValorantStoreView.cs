namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantStoreView(
    List<ValorantStoreOffer> Offers,
    long RemainingSeconds,
    List<ValorantStoreBundle> Bundles,
    // Absent la plupart du temps : le marché nocturne n'ouvre que par périodes.
    ValorantNightMarket? NightMarket
);