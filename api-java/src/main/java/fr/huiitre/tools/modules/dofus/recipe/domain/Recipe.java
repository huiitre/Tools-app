package fr.huiitre.tools.modules.dofus.recipe.domain;

public class Recipe {
    
    private final Long id;
    private final Long itemId;
    private final Long ingredientId;
    private final Long quantity;

    private Recipe(Long id, Long itemId, Long ingredientId, Long quantity) {
        this.id = id;
        this.itemId = itemId;
        this.ingredientId = ingredientId;
        this.quantity = quantity;
    }

    public static Recipe rehydrate(Long id, Long itemId, Long ingredientId, Long quantity) {
        return new Recipe(id, itemId, ingredientId, quantity);
    }

    public Long getId() {
        return id;
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