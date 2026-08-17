package fr.huiitre.tools.modules.dofus.workshop.application.dto;

import java.util.List;

import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;

public class WorkshopItemDetailDto {
    
    private final Long id;
    private final Long workshopId;
    private final ItemDto item;
    private final Long quantity; //? multiplicateur
    private final List<WorkshopIngredientDetailDto> ingredients;

    public WorkshopItemDetailDto(
        Long id,
        Long workshopId,
        ItemDto item,
        Long quantity,
        List<WorkshopIngredientDetailDto> ingredients
    ) {
        this.id = id;
        this.workshopId = workshopId;
        this.item = item;
        this.quantity = quantity;
        this.ingredients = ingredients;
    }

    public Long getId() {
        return id;
    }

    public Long getWorkshopId() {
        return workshopId;
    }

    public ItemDto getItem() {
        return item;
    }

    public Long getQuantity() {
        return quantity;
    }

    public List<WorkshopIngredientDetailDto> getIngredients() {
        return ingredients;
    }
}
