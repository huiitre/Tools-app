package fr.huiitre.tools.modules.riot.valorant.application.view;

import java.util.UUID;

public record ValorantWeaponView(Long id, UUID assetId, String name, String category, UUID defaultSkinAssetId, String displayIconUrl) {}
