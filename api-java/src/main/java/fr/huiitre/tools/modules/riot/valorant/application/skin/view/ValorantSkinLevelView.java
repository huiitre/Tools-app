package fr.huiitre.tools.modules.riot.valorant.application.skin.view;

import java.util.UUID;

public record ValorantSkinLevelView(UUID assetId, int levelIndex, String name, String levelItem, String displayIconUrl, String streamedVideoUrl) {}
