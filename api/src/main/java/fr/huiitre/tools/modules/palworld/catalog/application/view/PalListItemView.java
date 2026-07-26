package fr.huiitre.tools.modules.palworld.catalog.application.view;

import java.util.List;

public record PalListItemView(
        Long id,
        String tribe,
        Integer paldexIndex,
        String paldexSuffix,
        String name,
        String imageUrl,
        Integer rarity,
        String size,
        Integer baseHp,
        Integer baseAttack,
        Integer baseDefense,
        Integer baseWorkSpeed,
        Integer baseSupport,
        String bestWorkSuitabilityLabel,
        List<ElementSummaryView> elements,
        List<WorkSuitabilitySummaryView> workSuitabilities) {}
