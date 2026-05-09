package fr.huiitre.tools.modules.dofus.item.application.dto;

import java.util.List;

import fr.huiitre.tools.modules.dofus.itemtype.application.view.ItemTypeDto;

public class ItemLightDTO {
  
    private final Long id;
    private final String name;
    private final Long level;
    private final Long gameVersionId;
    private final Long assetId;

    private final ItemTypeDto itemType;
    private final List<ItemImageDto> images;

    public ItemLightDTO(
        Long id,
        String name,
        Long level,
        Long gameVersionId,
        Long assetId,
        ItemTypeDto itemType,
        List<ItemImageDto> images
    ) {
        this.id = id;
        this.name = name;
        this.level = level;
        this.gameVersionId = gameVersionId;
        this.assetId = assetId;
        this.itemType = itemType;
        this.images = images;
    }

    public Long getId() {
        return id;
    }

    public String getName() {
        return name;
    }

    public Long getLevel() {
        return level;
    }

    public Long getGameVersionId() {
        return gameVersionId;
    }

    public Long getAssetId() {
        return assetId;
    }

    public ItemTypeDto getItemType() {
        return itemType;
    }

    public List<ItemImageDto> getImages() {
        return images;
    }
}
