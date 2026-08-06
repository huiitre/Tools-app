package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.time.OffsetDateTime;
import java.util.List;
import java.util.Map;
import java.util.UUID;

public record ServerPalInventoryView(
        UUID instanceId,
        Long palId,
        UUID ownerPlayerUid,
        UUID baseId,
        String storageLocation,
        UUID containerId,
        String gender,
        Integer favoriteIndex,
        List<String> passiveSkillIds,
        Integer rank,
        Integer ivHp,
        Integer ivAttack,
        Integer ivDefense,
        java.math.BigDecimal currentHp,
        Integer baseHp,
        Integer baseMeleeAttack,
        Integer baseShotAttack,
        Integer baseDefense,
        Integer baseSupport,
        Integer baseCraftSpeed,
        Map<String, Integer> baseWorkSuitability,
        Map<String, Integer> workSuitabilityAddRanks,
        Integer level,
        OffsetDateTime lastSeenAt) {}
