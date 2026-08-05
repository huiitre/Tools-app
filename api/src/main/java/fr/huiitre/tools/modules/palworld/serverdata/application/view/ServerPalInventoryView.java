package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.time.OffsetDateTime;
import java.util.List;
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
        OffsetDateTime lastSeenAt) {}
