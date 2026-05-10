package fr.huiitre.tools.modules.riot.valorant.application.catalog.ports;

import java.util.List;

public interface ValorantStorePort {
    RawStorefront fetchStorefront(String puuid, String region, String accessToken, String entitlementsToken, String clientVersion);
    String fetchEntitlementsToken(String accessToken);

    record RawStorefront(
        List<RawOffer> singleItemOffers,
        long singleItemOffersRemainingDurationInSeconds,
        List<RawBundle> featuredBundles,
        RawNightMarket nightMarket
    ) {}

    record RawOffer(String itemId, int cost) {}

    record RawBundle(
        String assetId,
        List<RawOffer> items,
        int totalBaseCost,
        int totalDiscountedCost,
        int discountPercent,
        long remainingSeconds
    ) {}

    record RawNightMarket(
        List<RawNightMarketOffer> offers,
        long remainingSeconds
    ) {}

    record RawNightMarketOffer(
        String offerId,
        String itemId,
        int originalCost,
        int discountedCost,
        int discountPercent,
        boolean isSeen
    ) {}
}
