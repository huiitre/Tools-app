package fr.huiitre.tools.modules.dofus.pricing.application.view;

import java.time.LocalDateTime;

public class ItemPriceDto {

    private final Long itemId;
    private final Long[] parentItemIds;

    /* prix directs */
    private final Long userPrice;
    private final Long communityAveragePrice;
    private final Long lastUpdatedPrice;

    /* prix de craft */
    private final Long craftUserPrice;
    private final Long craftCommunityPrice;
    private final Long craftLastPrice;
    private final Long craftCalculatedPrice;

    private final LocalDateTime userPriceCreatedAt;
    private final LocalDateTime communityAveragePriceCreatedAt;
    private final LocalDateTime lastUpdatedPriceCreatedAt;

    public ItemPriceDto(
        Long itemId,
        Long[] parentItemIds,
        Long userPrice,
        Long communityAveragePrice,
        Long lastUpdatedPrice,
        Long craftUserPrice,
        Long craftCommunityPrice,
        Long craftLastPrice,
        Long craftCalculatedPrice,
        LocalDateTime userPriceCreatedAt,
        LocalDateTime communityAveragePriceCreatedAt,
        LocalDateTime lastUpdatedPriceCreatedAt
    ) {
        this.itemId = itemId;
        this.parentItemIds = parentItemIds;
        this.userPrice = userPrice;
        this.communityAveragePrice = communityAveragePrice;
        this.lastUpdatedPrice = lastUpdatedPrice;
        this.craftUserPrice = craftUserPrice;
        this.craftCommunityPrice = craftCommunityPrice;
        this.craftLastPrice = craftLastPrice;
        this.craftCalculatedPrice = craftCalculatedPrice;
        this.userPriceCreatedAt = userPriceCreatedAt;
        this.communityAveragePriceCreatedAt = communityAveragePriceCreatedAt;
        this.lastUpdatedPriceCreatedAt = lastUpdatedPriceCreatedAt;
    }

    public Long getItemId() {
        return itemId;
    }

    public Long[] getParentItemIds() {
        return parentItemIds;
    }

    public Long getUserPrice() {
        return userPrice;
    }

    public Long getCommunityAveragePrice() {
        return communityAveragePrice;
    }

    public Long getLastUpdatedPrice() {
        return lastUpdatedPrice;
    }

    public Long getCraftUserPrice() {
        return craftUserPrice;
    }

    public Long getCraftCommunityPrice() {
        return craftCommunityPrice;
    }

    public Long getCraftLastPrice() {
        return craftLastPrice;
    }

    public Long getCraftCalculatedPrice() {
        return craftCalculatedPrice;
    }

    public LocalDateTime getUserPriceCreatedAt() {
        return userPriceCreatedAt;
    }

    public LocalDateTime getCommunityAveragePriceCreatedAt() {
        return communityAveragePriceCreatedAt;
    }

    public LocalDateTime getLastUpdatedPriceCreatedAt() {
        return lastUpdatedPriceCreatedAt;
    }
}
