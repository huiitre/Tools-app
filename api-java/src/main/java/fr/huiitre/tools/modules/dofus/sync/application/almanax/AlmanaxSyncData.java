package fr.huiitre.tools.modules.dofus.sync.application.almanax;

import java.util.List;

public class AlmanaxSyncData {

    private final Long assetId;
    private final String name;
    private final String description;
    private final List<String> dates;
    private final Long itemId;
    private final Long itemQuantity;

    public AlmanaxSyncData(
            Long assetId,
            String name,
            String description,
            List<String> dates,
            Long itemId,
            Long itemQuantity) {
        this.assetId = assetId;
        this.name = name;
        this.description = description;
        this.dates = dates;
        this.itemId = itemId;
        this.itemQuantity = itemQuantity;
    }

    public Long getAssetId() {
        return assetId;
    }

    public String getName() {
        return name;
    }

    public String getDescription() {
        return description;
    }

    public List<String> getDates() {
        return dates;
    }

    public Long getItemId() {
        return itemId;
    }

    public Long getItemQuantity() {
        return itemQuantity;
    }
}
