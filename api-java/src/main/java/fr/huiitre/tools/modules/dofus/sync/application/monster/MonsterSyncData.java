package fr.huiitre.tools.modules.dofus.sync.application.monster;

import java.util.List;

public class MonsterSyncData {
    
    private final Long assetId;
    private final String name;
    private final Long iconId;
    private final List<Long> subareaIds;
    private final List<Long> itemIds;

    public MonsterSyncData(
            Long assetId,
            String name,
            Long iconId,
            List<Long> subareaIds,
            List<Long> itemIds) {
        this.assetId = assetId;
        this.name = name;
        this.iconId = iconId;
        this.subareaIds = subareaIds;
        this.itemIds = itemIds;
    }

    public Long getAssetId() {
        return assetId;
    }

    public String getName() {
        return name;
    }

    public Long getIconId() {
        return iconId;
    }

    public List<Long> getSubareaIds() {
        return subareaIds;
    }

    public List<Long> getItemIds() {
        return itemIds;
    }
}
