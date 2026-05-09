package fr.huiitre.tools.modules.dofus.sync.application.subarea;

public class SubareaSyncData {

    private final Long assetId;
    private final Long areaId;
    private final String name;

    public SubareaSyncData(
            Long assetId,
            Long areaId,
            String name) {
        this.assetId = assetId;
        this.areaId = areaId;
        this.name = name;
    }

    public Long getAssetId() {
        return assetId;
    }

    public Long getAreaId() {
        return areaId;
    }

    public String getName() {
        return name;
    }
}
