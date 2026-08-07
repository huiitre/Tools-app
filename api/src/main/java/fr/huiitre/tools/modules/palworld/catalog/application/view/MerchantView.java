package fr.huiitre.tools.modules.palworld.catalog.application.view;

import java.util.List;

public record MerchantView(
        Long id,
        String externalId,
        String code,
        String name,
        String portraitUrl,
        Integer restockMinute,
        ShopCurrencyView currency,
        List<MerchantOfferView> offers) {}
