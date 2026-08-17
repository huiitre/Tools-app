package fr.huiitre.tools.modules.palworld.sync.application.view;

import java.math.BigDecimal;

public record PalRefView(
        Long id,
        String tribe,
        Integer paldexIndex,
        String name,
        String size,
        Integer rarity,
        Integer baseHp,
        Integer baseAttack,
        Integer baseDefense,
        Integer baseWorkSpeed,
        Integer baseSupport,
        Integer runSpeed,
        Integer rideSprintSpeed,
        BigDecimal captureRateCorrect,
        BigDecimal maleProbability,
        Integer combiRank,
        Integer combiDuplicatePriority,
        boolean ignoreCombi,
        Integer price,
        String bestWorkSuitabilityLabel,
        String imageUrl,
        String description) {}
