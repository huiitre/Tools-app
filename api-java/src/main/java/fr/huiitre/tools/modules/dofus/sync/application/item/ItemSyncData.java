package fr.huiitre.tools.modules.dofus.sync.application.item;

public class ItemSyncData {
    private final Long assetId;
    private final String name;
    private final Long level;
    private final String description;
    private final Long itemTypeId;
    private final Long iconId;

    public ItemSyncData(
            Long assetId,
            String name,
            Long level,
            String description,
            Long itemTypeId,
            Long iconId) {
        this.assetId = assetId;
        this.name = name;
        this.level = level;
        this.description = description;
        this.itemTypeId = itemTypeId;
        this.iconId = iconId;
    }

    public Long getAssetId() {
        return assetId;
    }

    public String getName() {
        return name;
    }

    public Long getLevel() {
        return level;
    }

    public String getDescription() {
        return description;
    }

    public Long getItemTypeId() {
        return itemTypeId;
    }

    public Long getIconId() {
        return iconId;
    }
}
