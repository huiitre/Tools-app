package fr.huiitre.tools.modules.palworld.catalog.application.view;

public record ItemCatalogView(
        Long id,
        String slug,
        String name,
        String iconUrl,
        String category,
        Integer price,
        boolean soldByMerchant) {}
