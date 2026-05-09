package fr.huiitre.tools.modules.dofus.subarea.application.dto;

public class SubareaDto {
    
    private final Long id;
    private final Long areaId;
    private final String name;
    private final Long assetId;

    public SubareaDto(
            Long id,
            Long areaId,
            Long assetId,
            String name) {
        this.id = id;
        this.areaId = areaId;
        this.assetId = assetId;
        this.name = name;
    }

    public Long getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public Long getAreaId() {
        return areaId;
    }

    public Long getAssetId() {
        return assetId;
    }
}
