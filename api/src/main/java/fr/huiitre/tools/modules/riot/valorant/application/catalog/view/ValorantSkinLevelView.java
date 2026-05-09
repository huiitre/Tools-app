package fr.huiitre.tools.modules.riot.valorant.application.catalog.view;

import java.util.UUID;

public record ValorantSkinLevelView(UUID assetId, int levelIndex, String displayIconUrl, String streamedVideoUrl) {}
