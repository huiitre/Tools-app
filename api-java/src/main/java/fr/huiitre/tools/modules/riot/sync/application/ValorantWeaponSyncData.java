package fr.huiitre.tools.modules.riot.sync.application;

import java.util.UUID;

public class ValorantWeaponSyncData {

    private final UUID assetId;
    private final String name;
    private final String category;
    private final UUID defaultSkinAssetId;
    private final String displayIconUrl;

    public ValorantWeaponSyncData(UUID assetId, String name, String category, UUID defaultSkinAssetId, String displayIconUrl) {
        this.assetId = assetId;
        this.name = name;
        this.category = category;
        this.defaultSkinAssetId = defaultSkinAssetId;
        this.displayIconUrl = displayIconUrl;
    }

    public UUID getAssetId() { return assetId; }
    public String getName() { return name; }
    public String getCategory() { return category; }
    public UUID getDefaultSkinAssetId() { return defaultSkinAssetId; }
    public String getDisplayIconUrl() { return displayIconUrl; }
}
