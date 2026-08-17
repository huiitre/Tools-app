package fr.huiitre.tools.modules.riot.valorant.application.catalog.view;

import java.util.List;

public record ValorantNightMarket(
    List<ValorantNightMarketOffer> offers,
    long remainingSeconds
) {}
