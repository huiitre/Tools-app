package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.UUID;

public record PalInstanceSummaryView(
        UUID instanceId,
        String characterId,
        Long palId,
        String palName,
        String palImageUrl,
        Integer palFoodAmount,
        boolean isAlpha,
        UUID ownerPlayerUid,
        Integer level,
        Integer exp,
        BigDecimal fullStomach,
        Boolean isSick,
        String workableType,
        String taskId,
        Integer workState,
        BigDecimal currentWorkAmount,
        BigDecimal requiredWorkAmount,
        OffsetDateTime firstSeenAt,
        OffsetDateTime lastSeenAt) {}
