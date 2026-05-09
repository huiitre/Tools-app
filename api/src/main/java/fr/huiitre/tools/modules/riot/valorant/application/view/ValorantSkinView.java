package fr.huiitre.tools.modules.riot.valorant.application.view;

import java.util.List;
import java.util.UUID;

public record ValorantSkinView(Long id, UUID assetId, String name, String iconUrl, UUID tierUuid, UUID contentTierUuid, Long weaponId, List<ValorantSkinLevelView> levels) {}
