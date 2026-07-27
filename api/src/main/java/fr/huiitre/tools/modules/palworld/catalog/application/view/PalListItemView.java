package fr.huiitre.tools.modules.palworld.catalog.application.view;

import java.math.BigDecimal;
import java.util.List;

public record PalListItemView(
        Long id,
        String tribe,
        Integer paldexIndex,
        String paldexSuffix,
        String name,
        String imageUrl,
        String description,
        Integer rarity,
        String size,
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
        String bestWorkSuitabilityLabel,
        List<ElementSummaryView> elements,
        List<WorkSuitabilitySummaryView> workSuitabilities,
        List<PassiveSkillSummaryView> passiveSkills,
        List<ActiveSkillSummaryView> activeSkills,
        PartnerSkillSummaryView partnerSkill,
        List<DropSummaryView> drops,
        List<VariantSummaryView> variants,
        List<SpawnZoneSummaryView> spawnZones) {}
