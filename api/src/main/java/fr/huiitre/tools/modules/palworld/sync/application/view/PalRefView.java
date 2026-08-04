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
<<<<<<< Updated upstream
        Integer goldCoin,
        String eggType,
        String bestWorkSuitabilityLabel,
        Integer foodGaugeFilled,
        Integer foodGaugeEmpty,
        String foodGaugeIconUrl) {}
=======
        Integer price,
        String bestWorkSuitabilityLabel) {}
>>>>>>> Stashed changes
