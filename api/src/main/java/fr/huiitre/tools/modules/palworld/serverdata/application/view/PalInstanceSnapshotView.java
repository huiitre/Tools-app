package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

public record PalInstanceSnapshotView(
        OffsetDateTime capturedAt,
        Integer level,
        Integer exp,
        BigDecimal fullStomach,
        Boolean isSick,
        String workableType,
        String taskId,
        Integer workState,
        BigDecimal currentWorkAmount,
        BigDecimal requiredWorkAmount) {}
