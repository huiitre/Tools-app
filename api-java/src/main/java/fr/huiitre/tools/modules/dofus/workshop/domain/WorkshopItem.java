package fr.huiitre.tools.modules.dofus.workshop.domain;

public class WorkshopItem {
    
    private final Long id;
    private final Long itemId;
    private Long quantity;

    private WorkshopItem(Long id, Long itemId, Long quantity) {

        validateQuantity(quantity);

        this.id = id;
        this.itemId = itemId;
        this.quantity = quantity;
    }

    public static WorkshopItem rehydrate(Long id, Long itemId, Long quantity) {
        return new WorkshopItem(id, itemId, quantity);
    }

    public static WorkshopItem create(Long itemId, Long quantity) {
        return new WorkshopItem(null, itemId, quantity);
    }

    public void update(Long quantity) {
        validateQuantity(quantity);
        this.quantity = quantity;
    }

    private void validateQuantity(Long quantity) {
        if (quantity == null || quantity < 0) {
            throw new IllegalArgumentException("La quantité doit être supérieure ou égale à 0.");
        }
    }

    public Long getId() {
        return id;
    }

    public Long getQuantity() {
        return quantity;
    }

    public Long getItemId() {
        return itemId;
    }
}
