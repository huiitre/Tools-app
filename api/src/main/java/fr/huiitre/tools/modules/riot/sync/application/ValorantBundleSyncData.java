package fr.huiitre.tools.modules.riot.sync.application;

import java.util.UUID;

public class ValorantBundleSyncData {

    private final UUID assetId;
    private final String name;
    private final String bannerUrl;

    public ValorantBundleSyncData(UUID assetId, String name, String bannerUrl) {
        this.assetId = assetId;
        this.name = name;
        this.bannerUrl = bannerUrl;
    }

    public UUID getAssetId() { return assetId; }
    public String getName() { return name; }
    public String getBannerUrl() { return bannerUrl; }
}
