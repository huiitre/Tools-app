package fr.huiitre.tools.modules.palworld.catalog.application.view;

public record MerchantOfferView(
        Long itemId,
        String itemSlug,
        String itemName,
        String itemIconUrl,
        Integer itemMaxStackCount,
        int price,
        int quantityPerPurchase,
        String productType) {}
