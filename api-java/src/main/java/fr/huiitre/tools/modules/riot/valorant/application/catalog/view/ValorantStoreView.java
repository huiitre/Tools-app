package fr.huiitre.tools.modules.riot.valorant.application.catalog.view;

import java.util.List;

public record ValorantStoreView(
    List<ValorantStoreOffer> offers,
    long remainingSeconds,
    List<ValorantStoreBundle> bundles,
    ValorantNightMarket nightMarket
) {}
