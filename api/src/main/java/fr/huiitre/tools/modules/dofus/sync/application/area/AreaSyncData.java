package fr.huiitre.tools.modules.dofus.sync.application.area;

public class AreaSyncData {

    private final Long assetId;
    private final String name;

    public AreaSyncData(
            Long assetId,
            String name) {
        this.assetId = assetId;
        this.name = name;
    }

    public Long getAssetId() {
        return assetId;
    }

    public String getName() {
        return name;
    }
}
