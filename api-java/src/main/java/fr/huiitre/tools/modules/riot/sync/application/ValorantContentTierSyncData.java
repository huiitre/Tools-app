package fr.huiitre.tools.modules.riot.sync.application;

import java.util.UUID;

public class ValorantContentTierSyncData {

    private final UUID assetId;
    private final String name;
    private final String devName;
    private final int rank;
    private final int juiceValue;
    private final int juiceCost;
    private final String highlightColor;
    private final String displayIconUrl;

    public ValorantContentTierSyncData(UUID assetId, String name, String devName, int rank,
            int juiceValue, int juiceCost, String highlightColor, String displayIconUrl) {
        this.assetId = assetId;
        this.name = name;
        this.devName = devName;
        this.rank = rank;
        this.juiceValue = juiceValue;
        this.juiceCost = juiceCost;
        this.highlightColor = highlightColor;
        this.displayIconUrl = displayIconUrl;
    }

    public UUID getAssetId() { return assetId; }
    public String getName() { return name; }
    public String getDevName() { return devName; }
    public int getRank() { return rank; }
    public int getJuiceValue() { return juiceValue; }
    public int getJuiceCost() { return juiceCost; }
    public String getHighlightColor() { return highlightColor; }
    public String getDisplayIconUrl() { return displayIconUrl; }
}