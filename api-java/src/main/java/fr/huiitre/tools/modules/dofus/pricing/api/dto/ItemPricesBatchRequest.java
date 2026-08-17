package fr.huiitre.tools.modules.dofus.pricing.api.dto;

public class ItemPricesBatchRequest {
    
    private final Long itemId;
    private final Long price;

    public ItemPricesBatchRequest(Long itemId, Long price) {
        this.itemId = itemId;
        this.price = price;
    }

    public Long getItemId() {
        return itemId;
    }

    public Long getPrice() {
        return price;
    }
}
