package fr.huiitre.tools.modules.riot.valorant.application.catalog.view;

import java.util.List;

public record ValorantStoreBundle(
    String assetId,
    String name,
    String bannerUrl,
    List<ValorantStoreOffer> items,
    int totalBaseCost,
    int totalDiscountedCost,
    int discountPercent,
    long remainingSeconds
) {}
