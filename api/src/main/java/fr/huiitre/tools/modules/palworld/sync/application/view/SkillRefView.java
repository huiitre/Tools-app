package fr.huiitre.tools.modules.palworld.sync.application.view;

import java.math.BigDecimal;

public record SkillRefView(
        Long id,
        String slug,
        String category,
        String name,
        String iconUrl,
        Long elementId,
        BigDecimal cooldown,
        Integer power,
        String statusEffect,
        String description) {}
