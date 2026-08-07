package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.sync.application.MerchantOfferSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.MerchantSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.MerchantSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.ports.MerchantDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.MerchantSyncRepository;

@Service
@Transactional
public class SyncMerchantsUseCase implements SecuredUseCase {

    private static final Logger log = LoggerFactory.getLogger(SyncMerchantsUseCase.class);

    private final MerchantDataProvider dataProvider;
    private final MerchantSyncRepository syncRepository;

    public SyncMerchantsUseCase(MerchantDataProvider dataProvider, MerchantSyncRepository syncRepository) {
        this.dataProvider = dataProvider;
        this.syncRepository = syncRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    // Doit tourner après SyncItemsUseCase (itemIdBySlugUpper doit couvrir tous les offers[].itemId).
    public MerchantSyncReport execute(Map<String, Long> itemIdBySlugUpper) {
        List<MerchantSyncData> external = dataProvider.fetchAll();

        int merchantsSynced = 0;
        int offersSynced = 0;
        int offersSkipped = 0;

        for (MerchantSyncData merchant : external) {
            Long merchantId = syncRepository.upsertMerchant(
                    merchant.externalId(), merchant.code(), merchant.name(), merchant.portraitUrl(),
                    merchant.restockMinute(), merchant.currencyItemId());
            merchantsSynced++;

            syncRepository.deleteOffers(merchantId);
            for (MerchantOfferSyncData offer : merchant.offers()) {
                Long itemId = itemIdBySlugUpper.get(offer.itemSlug().toUpperCase());
                if (itemId == null) {
                    log.warn("Palworld merchant offer ignorée, item introuvable dans le catalogue: merchant={} item={}",
                            merchant.externalId(), offer.itemSlug());
                    offersSkipped++;
                    continue;
                }
                if (syncRepository.insertOffer(merchantId, itemId, offer.price(), offer.quantityPerPurchase(), offer.productType())) {
                    offersSynced++;
                } else {
                    offersSkipped++;
                }
            }
        }

        Set<String> externalIds = external.stream().map(MerchantSyncData::externalId).collect(Collectors.toSet());
        int merchantsDeleted = syncRepository.deleteAllNotIn(externalIds);

        log.info("Palworld merchants sync: {} merchants, {} offers synced, {} offers skipped, {} merchants deleted",
                merchantsSynced, offersSynced, offersSkipped, merchantsDeleted);
        return new MerchantSyncReport(merchantsSynced, merchantsDeleted, offersSynced, offersSkipped);
    }
}
