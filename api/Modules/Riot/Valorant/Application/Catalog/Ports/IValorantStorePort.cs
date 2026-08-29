namespace Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;

// La boutique vient de Riot, pas de la base : les identifiants renvoyés sont des UUID de *levels*
// de skins, résolus ensuite contre le catalogue local.
public interface IValorantStorePort
{
    Task<RawStorefront> FetchStorefront(
        string puuid,
        string region,
        string accessToken,
        string entitlementsToken,
        string clientVersion
    );

    Task<string> FetchEntitlementsToken(string accessToken);

    Task<RiotId> FetchRiotId(
        string puuid,
        string region,
        string accessToken,
        string entitlementsToken,
        string clientVersion
    );

    // Nuls quand Riot n'a pas répondu : le pseudo est un confort, pas une donnée requise.
    record RiotId(string? GameName, string? TagLine);

    record RawStorefront(
        List<RawOffer> SingleItemOffers,
        long SingleItemOffersRemainingDurationInSeconds,
        List<RawBundle> FeaturedBundles,
        RawNightMarket? NightMarket
    );

    record RawOffer(string ItemId, int Cost);

    record RawBundle(
        string AssetId,
        List<RawOffer> Items,
        int TotalBaseCost,
        int TotalDiscountedCost,
        int DiscountPercent,
        long RemainingSeconds
    );

    record RawNightMarket(
        List<RawNightMarketOffer> Offers,
        long RemainingSeconds
    );

    record RawNightMarketOffer(
        string OfferId,
        string ItemId,
        int OriginalCost,
        int DiscountedCost,
        int DiscountPercent,
        bool IsSeen
    );
}
