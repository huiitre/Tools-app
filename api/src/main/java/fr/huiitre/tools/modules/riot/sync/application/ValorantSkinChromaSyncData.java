package fr.huiitre.tools.modules.riot.sync.application;

import java.util.UUID;

public class ValorantSkinChromaSyncData {

    private final UUID assetId;
    private final int chromaIndex;
    private final String name;
    private final String displayIconUrl;
    private final String fullRenderUrl;
    private final String swatchUrl;
    private final String streamedVideoUrl;

    public ValorantSkinChromaSyncData(UUID assetId, int chromaIndex, String name, String displayIconUrl, String fullRenderUrl, String swatchUrl, String streamedVideoUrl) {
        this.assetId = assetId;
        this.chromaIndex = chromaIndex;
        this.name = name;
        this.displayIconUrl = displayIconUrl;
        this.fullRenderUrl = fullRenderUrl;
        this.swatchUrl = swatchUrl;
        this.streamedVideoUrl = streamedVideoUrl;
    }

    public UUID getAssetId() { return assetId; }
    public int getChromaIndex() { return chromaIndex; }
    public String getName() { return name; }
    public String getDisplayIconUrl() { return displayIconUrl; }
    public String getFullRenderUrl() { return fullRenderUrl; }
    public String getSwatchUrl() { return swatchUrl; }
    public String getStreamedVideoUrl() { return streamedVideoUrl; }
}
