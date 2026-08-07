package fr.huiitre.tools.modules.palworld.sync.application;

import java.util.List;

public record MerchantSyncData(
        String externalId,
        String code,
        String name,
        String portraitUrl,
        Integer restockMinute,
        String currencyItemId,
        List<MerchantOfferSyncData> offers) {}
