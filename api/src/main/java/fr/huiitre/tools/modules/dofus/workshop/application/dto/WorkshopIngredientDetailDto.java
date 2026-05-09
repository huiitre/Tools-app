package fr.huiitre.tools.modules.dofus.workshop.application.dto;

import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;

public class WorkshopIngredientDetailDto {
    
    private final Long id;
    private final Long workshopItemId;
    private final ItemDto item;
    private final Long quantityObtained;
    private final Long quantityRequired;

    private final Long parentIngredientId;

    public WorkshopIngredientDetailDto(
        Long id,
        Long workshopItemId,
        ItemDto item,
        Long parentIngredientId,
        Long quantityObtained,
        Long quantityRequired
    ) {
        this.id = id;
        this.workshopItemId = workshopItemId;
        this.item = item;
        this.parentIngredientId = parentIngredientId;
        this.quantityObtained = quantityObtained;
        this.quantityRequired = quantityRequired;
    }

    public Long getId() {
        return id;
    }

    public Long getWorkshopItemId() {
        return workshopItemId;
    }

    public ItemDto getItem() {
        return item;
    }

    public Long getParentIngredientId() {
        return parentIngredientId;
    }

    public Long getQuantityObtained() {
        return quantityObtained;
    }

    public Long getQuantityRequired() {
        return quantityRequired;
    }
}