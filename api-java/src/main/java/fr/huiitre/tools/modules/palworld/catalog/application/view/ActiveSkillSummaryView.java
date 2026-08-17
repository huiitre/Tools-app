package fr.huiitre.tools.modules.palworld.catalog.application.view;

import java.math.BigDecimal;

public record ActiveSkillSummaryView(
        Long id,
        String slug,
        String category,
        String name,
        String iconUrl,
        ElementSummaryView element,
        BigDecimal cooldown,
        Integer power,
        String statusEffect,
        String description,
        int unlockLevel) {}
