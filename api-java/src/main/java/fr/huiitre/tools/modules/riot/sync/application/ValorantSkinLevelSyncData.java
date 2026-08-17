package fr.huiitre.tools.modules.riot.sync.application;

import java.util.UUID;

public class ValorantSkinLevelSyncData {

    private final UUID assetId;
    private final int levelIndex;
    private final String name;
    private final String levelItem;
    private final String displayIconUrl;
    private final String streamedVideoUrl;

    public ValorantSkinLevelSyncData(UUID assetId, int levelIndex, String name, String levelItem, String displayIconUrl, String streamedVideoUrl) {
        this.assetId = assetId;
        this.levelIndex = levelIndex;
        this.name = name;
        this.levelItem = levelItem;
        this.displayIconUrl = displayIconUrl;
        this.streamedVideoUrl = streamedVideoUrl;
    }

    public UUID getAssetId() { return assetId; }
    public int getLevelIndex() { return levelIndex; }
    public String getName() { return name; }
    public String getLevelItem() { return levelItem; }
    public String getDisplayIconUrl() { return displayIconUrl; }
    public String getStreamedVideoUrl() { return streamedVideoUrl; }
}
