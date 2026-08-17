package fr.huiitre.tools.modules.dofus.itemtype.application.view;

public class ItemTypeDto {
    private final Long id;
    private final Long assetId;
    private final Long gameVersionId;
    private final String name;

    public ItemTypeDto(
            Long id,
            Long assetId,
            Long gameVersionId,
            String name) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.name = name;
    }

    public Long getId() {
        return id;
    }

    public Long getAssetId() {
        return assetId;
    }

    public Long getGameVersionId() {
        return gameVersionId;
    }

    public String getName() {
        return name;
    }
}
