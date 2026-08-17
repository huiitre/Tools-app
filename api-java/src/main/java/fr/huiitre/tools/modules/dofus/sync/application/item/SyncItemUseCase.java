package fr.huiitre.tools.modules.dofus.sync.application.item;

import java.util.ArrayList;
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
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.domain.Item;
import fr.huiitre.tools.modules.dofus.itemtype.application.ports.ItemTypeRepository;
import fr.huiitre.tools.modules.dofus.itemtype.domain.ItemType;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncItemUseCase implements SecuredUseCase {

        private final static Logger logger = LoggerFactory.getLogger(SyncItemUseCase.class);

        private final List<ItemDataProvider> itemDataProviders;
        private final ItemRepository itemRepository;
        private final ItemTypeRepository itemTypeRepository;

        @Override
        public Optional<ModuleCode> requiredModule() {
                return Optional.of(ModuleCode.DOFUS);
        }

        @Override
        public RoleCode requiredRole() {
                return RoleCode.TECH;
        }

        public SyncItemUseCase(
                        ItemRepository itemRepository,
                        List<ItemDataProvider> itemDataProviders,
                        ItemTypeRepository itemTypeRepository) {
                this.itemDataProviders = itemDataProviders;
                this.itemRepository = itemRepository;
                this.itemTypeRepository = itemTypeRepository;
        }

        public SyncReport execute(GameVersionData gameVersion) {

                ItemDataProvider itemDataProvider = itemDataProviders.stream()
                                .filter(p -> p.supports(gameVersion.getCode()))
                                .findFirst()
                                .orElseThrow(() -> new IllegalArgumentException("No ItemDataProvider found for version: " + gameVersion.getCode()));

                List<ItemSyncData> external = itemDataProvider.fetchAll();

                Map<Long, Long> assetIdCounts = external.stream()
                                .collect(Collectors.groupingBy(
                                                ItemSyncData::getAssetId,
                                                Collectors.counting()));

                assetIdCounts.entrySet().stream()
                                .filter(e -> e.getValue() > 1)
                                .forEach(e -> logger.error(
                                                "DUPLICATE asset_id in external: assetId={} count={}",
                                                e.getKey(),
                                                e.getValue()));

                List<Item> current = itemRepository.findAllByGameVersionId(gameVersion.getId());

                Map<Long, Item> currentByAssetId = current.stream().collect(
                                Collectors
                                                .toMap(
                                                                Item::getAssetId,
                                                                it -> it));

                List<String> created = new ArrayList<>();
                List<String> updated = new ArrayList<>();

                List<ItemType> itemTypes = itemTypeRepository.findAllByGameVersionId(gameVersion.getId());

                Map<Long, Long> itemTypeIdByAssetId = itemTypes.stream()
                                .collect(Collectors.toMap(
                                                ItemType::getAssetId,
                                                ItemType::getId));

                for (ItemSyncData ext : external) {

                        Item existing = currentByAssetId.get(ext.getAssetId());

                        Long itemTypeId = Optional.ofNullable(
                                        itemTypeIdByAssetId.get(ext.getItemTypeId()))
                                        .orElseThrow(() -> new IllegalArgumentException(
                                                        "Item type not found for assetId: " + ext.getItemTypeId()));

                        if (existing == null) {
                                Item createdItem = Item.create(
                                                ext.getAssetId(),
                                                gameVersion.getId(),
                                                itemTypeId,
                                                ext.getName(),
                                                ext.getLevel(),
                                                ext.getDescription());
                                Long saved = itemRepository.save(createdItem);
                                created.add(
                                                "assetId=" + ext.getAssetId()
                                                                + " name=\"" + ext.getName() + "\""
                                                                + " itemTypeId=" + itemTypeId
                                                                + " level=" + ext.getLevel()
                                                                + " description=\"" + ext.getDescription() + "\""
                                                                + " iconId=" + ext.getIconId());

                                itemRepository.refreshImages(
                                    saved,
                                    ext.getIconId(),
                                    gameVersion
                                );

                                continue;
                        }

                        boolean nameChanged = !existing.getName().equals(ext.getName());
                        boolean itemTypeChanged = !existing.getItemTypeId().equals(itemTypeId);
                        boolean levelChanged = !existing.getLevel().equals(ext.getLevel());
                        boolean descriptionChanged = !existing.getDescription().equals(ext.getDescription());

                        if (nameChanged || itemTypeChanged || levelChanged || descriptionChanged) {

                                String oldName = existing.getName();
                                Long oldItemTypeId = existing.getItemTypeId();
                                Long oldLevel = existing.getLevel();
                                String oldDescription = existing.getDescription();

                                existing.update(
                                                itemTypeId,
                                                ext.getName(),
                                                ext.getLevel(),
                                                ext.getDescription());

                                itemRepository.update(existing);

                                updated.add(
                                                "assetId=" + ext.getAssetId()
                                                                + (nameChanged ? " name: \"" + oldName + "\" -> \""
                                                                                + ext.getName() + "\"" : "")
                                                                + (itemTypeChanged ? " itemTypeId: " + oldItemTypeId
                                                                                + " -> " + ext.getItemTypeId()
                                                                                : "")
                                                                + (levelChanged ? " level: " + oldLevel + " -> "
                                                                                + ext.getLevel() : "")
                                                                + (descriptionChanged
                                                                                ? " description: \"" + oldDescription
                                                                                                + "\" -> \""
                                                                                                + ext.getDescription()
                                                                                                + "\""
                                                                                : ""));

                                itemRepository.refreshImages(
                                                existing.getId(),
                                                ext.getIconId(),
                                                gameVersion);
                        }
                }

                return new SyncReport(
                                "items",
                                "Items",
                                created,
                                updated);
        }
}