package fr.huiitre.tools.modules.riot.valorant.application.skin.view;

import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;

public record ValorantSkinView(
    Long id,
    UUID assetId,
    String name,
    String iconUrl,
    UUID tierUuid,
    UUID contentTierUuid,
    Long weaponId,
    List<ValorantSkinLevelView> levels,
    List<ValorantSkinChromaView> chromas,
    boolean owned,
    boolean watched,
    LocalDateTime ownedAt,
    LocalDateTime watchedAt
) {}
