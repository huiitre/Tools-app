package fr.huiitre.tools.modules.dofus.item.application.dto;

import java.util.List;

import fr.huiitre.tools.modules.dofus.itemtype.application.view.ItemTypeDto;

public class ItemDto {

    private final Long id;
    private final String name;
    private final String description;
    private final boolean hasRecipe;
    private final Long assetId;
    private final Long gameVersionId;
    private final Long level;

    private final ItemTypeDto itemType;
    private final List<ItemImageDto> images;

    private final Long parentItemId;
    private final Long quantity;
    private final List<FarmZoneDto> farmZones;

    public ItemDto(
            Long id,
            String name,
            String description,
            boolean hasRecipe,
            Long assetId,
            Long gameVersionId,
            Long level,
            ItemTypeDto type,
            List<ItemImageDto> images,
            Long parentItemId,
            Long quantity,
            List<FarmZoneDto> farmZones) {
        this.id = id;
        this.assetId = assetId;
        this.gameVersionId = gameVersionId;
        this.name = name;
        this.level = level;
        this.description = description;
        this.itemType = type;
        this.images = images;
        this.hasRecipe = hasRecipe;
        this.parentItemId = parentItemId;
        this.quantity = quantity;
        this.farmZones = farmZones;
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

    public Long getLevel() {
        return level;
    }

    public String getDescription() {
        return description;
    }

    public ItemTypeDto getType() {
        return itemType;
    }

    public List<ItemImageDto> getImages() {
        return images;
    }

    public boolean isHasRecipe() {
        return hasRecipe;
    }

    public Long getParentItemId() {
        return parentItemId;
    }

    public Long getQuantity() {
        return quantity;
    }

    public List<FarmZoneDto> getFarmZones() {
        return farmZones;
    }
}