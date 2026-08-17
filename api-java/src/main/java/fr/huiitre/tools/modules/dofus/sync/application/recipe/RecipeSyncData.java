package fr.huiitre.tools.modules.dofus.sync.application.recipe;

public class RecipeSyncData {
    
    private final Long itemId;
    private final Long ingredientId;
    private final Long quantity;

    public RecipeSyncData(
        Long itemId,
        Long ingredientId,
        Long quantity
    ) {
        this.itemId = itemId;
        this.ingredientId = ingredientId;
        this.quantity = quantity;
    }

    public Long getItemId() {
        return itemId;
    }

    public Long getIngredientId() {
        return ingredientId;
    }

    public Long getQuantity() {
        return quantity;
    }
}
