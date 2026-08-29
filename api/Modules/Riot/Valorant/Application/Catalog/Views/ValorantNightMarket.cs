namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Views;

public sealed record ValorantNightMarket(
    List<ValorantNightMarketOffer> Offers,
    long RemainingSeconds
);