package fr.huiitre.tools.modules.riot.valorant.application.skin.view;

import java.util.UUID;

public record ValorantSkinChromaView(
    UUID assetId,
    int chromaIndex,
    String name,
    String displayIconUrl,
    String fullRenderUrl,
    String swatchUrl,
    String streamedVideoUrl
) {}
