package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.Set;

public interface MerchantSyncRepository {

    Long upsertMerchant(
            String externalId, String code, String name, String portraitUrl, Integer restockMinute, String currencyItemId);

    int deleteAllNotIn(Set<String> externalIds);

    void deleteOffers(Long merchantId);

    boolean insertOffer(Long merchantId, Long itemId, int price, int quantityPerPurchase, String productType);
}
