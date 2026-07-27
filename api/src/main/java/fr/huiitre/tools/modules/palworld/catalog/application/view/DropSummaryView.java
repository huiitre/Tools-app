package fr.huiitre.tools.modules.palworld.catalog.application.view;

import java.math.BigDecimal;

public record DropSummaryView(
        Long itemId,
        String itemSlug,
        String itemName,
        String itemIconUrl,
        Integer quantityMin,
        Integer quantityMax,
        BigDecimal probabilityPercent,
        String levelLabel) {}
