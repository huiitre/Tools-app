package fr.huiitre.tools.modules.riot.sync.application;

import java.util.List;
import java.util.UUID;

public class ValorantSkinSyncData {

    private final UUID assetId;
    private final String name;
    private final String iconUrl;
    private final UUID tierUuid;
    private final UUID contentTierUuid;
    private final UUID weaponAssetId;
    private final List<ValorantSkinLevelSyncData> levels;
    private final List<ValorantSkinChromaSyncData> chromas;

    public ValorantSkinSyncData(UUID assetId, String name, String iconUrl, UUID tierUuid, UUID contentTierUuid, UUID weaponAssetId, List<ValorantSkinLevelSyncData> levels, List<ValorantSkinChromaSyncData> chromas) {
        this.assetId = assetId;
        this.name = name;
        this.iconUrl = iconUrl;
        this.tierUuid = tierUuid;
        this.contentTierUuid = contentTierUuid;
        this.weaponAssetId = weaponAssetId;
        this.levels = levels;
        this.chromas = chromas;
    }

    public UUID getAssetId() { return assetId; }
    public String getName() { return name; }
    public String getIconUrl() { return iconUrl; }
    public UUID getTierUuid() { return tierUuid; }
    public UUID getContentTierUuid() { return contentTierUuid; }
    public UUID getWeaponAssetId() { return weaponAssetId; }
    public List<ValorantSkinLevelSyncData> getLevels() { return levels; }
    public List<ValorantSkinChromaSyncData> getChromas() { return chromas; }
}
