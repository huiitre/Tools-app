package fr.huiitre.tools.modules.riot.valorant.application.catalog.view;

import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

public record ValorantStoreOffer(
    ValorantSkinView skin,
    int cost
) {}
