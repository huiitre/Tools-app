package fr.huiitre.tools.modules.riot.valorant.application.catalog.view;

import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

public record ValorantNightMarketOffer(
    String offerId,
    ValorantSkinView skin,
    int originalCost,
    int discountedCost,
    int discountPercent,
    boolean isSeen
) {}
