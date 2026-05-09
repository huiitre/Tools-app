package fr.huiitre.tools.modules.dofus.workshop.domain;

public class WorkshopItemIngredient {
    
    private final Long id;
    private final Long workshopItemId;
    private final Long itemId;
    private final Long parentIngredientId;
    private Long quantityObtained;

    private WorkshopItemIngredient(Long id, Long workshopItemId, Long itemId, Long parentIngredientId, Long quantityObtained) {
        validateQuantity(quantityObtained);

        this.id = id;
        this.workshopItemId = workshopItemId;
        this.itemId = itemId;
        this.parentIngredientId = parentIngredientId;
        this.quantityObtained = quantityObtained;
    }

    public static WorkshopItemIngredient rehydrate(Long id, Long workshopItemId, Long itemId, Long parentIngredientId, Long quantityObtained) {
        return new WorkshopItemIngredient(id, workshopItemId, itemId, parentIngredientId, quantityObtained);
    }

    public static WorkshopItemIngredient create(
        Long workshopItemId,
        Long itemId,
        Long parentIngredientId,
        Long quantityObtained
    ) {
        return new WorkshopItemIngredient(null, workshopItemId, itemId, parentIngredientId, quantityObtained);
    }

    public void update(Long quantityObtained) {
        validateQuantity(quantityObtained);
        this.quantityObtained = quantityObtained;
    }

    private void validateQuantity(Long quantity) {
        if (quantity == null || quantity < 0) {
            throw new IllegalArgumentException("La quantité doit être supérieure ou égale à 0.");
        }
    }

    public Long getId() {
        return id;
    }

    public Long getQuantityObtained() {
        return quantityObtained;
    }

    public Long getItemId() {
        return itemId;
    }
    
    public Long getWorkshopItemId() {
        return workshopItemId;
    }

    public Long getParentIngredientId() {
        return parentIngredientId;
    }
}