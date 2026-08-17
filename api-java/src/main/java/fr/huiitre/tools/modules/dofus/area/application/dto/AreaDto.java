package fr.huiitre.tools.modules.dofus.area.application.dto;

public class AreaDto {
    
    private final Long id;
    private final Long assetId;
    private final String name;

    public AreaDto(
        Long id,
        Long assetId,
        String name
    ) {
        this.id = id;
        this.assetId = assetId;
        this.name = name;
    }

    public Long getId() {
        return id;
    }

    public Long getAssetId() {
        return assetId;
    }

    public String getName() {
        return name;
    }
}
