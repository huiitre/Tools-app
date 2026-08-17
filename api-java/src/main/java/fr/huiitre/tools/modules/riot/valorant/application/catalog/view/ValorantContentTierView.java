package fr.huiitre.tools.modules.riot.valorant.application.catalog.view;

import java.util.UUID;

public record ValorantContentTierView(
        Long id,
        UUID assetId,
        String name,
        String devName,
        int rank,
        int juiceValue,
        int juiceCost,
        String highlightColor,
        String displayIconUrl) {}