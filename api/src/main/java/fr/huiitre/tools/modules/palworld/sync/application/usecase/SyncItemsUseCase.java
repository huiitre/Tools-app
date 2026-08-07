package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.sync.application.ItemSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ItemDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ItemSyncRepository;

@Service
@Transactional
public class SyncItemsUseCase implements SecuredUseCase {

    private static final Logger log = LoggerFactory.getLogger(SyncItemsUseCase.class);

    private final ItemDataProvider dataProvider;
    private final ItemSyncRepository syncRepository;

    public SyncItemsUseCase(ItemDataProvider dataProvider, ItemSyncRepository syncRepository) {
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

    // Catalogue complet et autoritaire (item_data.json, ~2466 items) : source de vérité unique pour
    // name/icon_url/price/max_stack_count. Doit tourner en tout premier dans SyncPalworldUseCase — les drops
    // de Pals (PostgresPalSyncRepository.findOrCreateItem) et les offres marchands (SyncMerchantsUseCase) ne
    // font ensuite que résoudre un lien vers ce catalogue déjà peuplé, jamais créer/dégrader un item eux-mêmes.
    // Upsert uniquement, jamais de suppression (un item disparu d'un extract ne doit pas casser les FK
    // pal_drop/merchant_offer existantes).
    public Map<String, Long> execute() {
        List<ItemSyncData> external = dataProvider.fetchAll();
        // Clé en majuscule : même réserve de casse que palIdByTribeUpper (cf. SyncPalsUseCase) — au moins un
        // cas connu ("egg" côté offre marchand vs "Egg" côté catalogue), résolu par un lookup insensible à la casse
        // plutôt qu'une normalisation risquée des données sources.
        Map<String, Long> itemIdBySlugUpper = external.stream()
                .collect(Collectors.toMap(
                        item -> item.slug().toUpperCase(),
                        item -> syncRepository.upsertItem(
                                item.slug(), item.name(), item.iconUrl(), item.price(), item.maxStackCount(), item.category()),
                        (a, b) -> a));
        log.info("Palworld items catalog sync: {} upserted", itemIdBySlugUpper.size());
        return itemIdBySlugUpper;
    }
}
