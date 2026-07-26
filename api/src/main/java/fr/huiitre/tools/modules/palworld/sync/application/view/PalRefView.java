package fr.huiitre.tools.modules.palworld.sync.application.view;

import java.math.BigDecimal;

public record PalRefView(
        Long id,
        String tribe,
        Integer paldexIndex,
        String paldexSuffix,
        String name,
        String imageUrl,
        String description,
        String size,
        Integer rarity,
        Integer baseHp,
        Integer baseAttack,
        Integer baseDefense,
        Integer baseWorkSpeed,
        Integer baseSupport,
        Integer foodAmount,
        BigDecimal captureRateCorrect,
        BigDecimal maleProbability,
        Integer combiRank,
        Integer goldCoin,
        String eggType,
        String bestWorkSuitabilityLabel) {}
