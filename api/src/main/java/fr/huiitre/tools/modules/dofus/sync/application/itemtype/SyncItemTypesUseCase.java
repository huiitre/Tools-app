package fr.huiitre.tools.modules.dofus.sync.application.itemtype;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.itemtype.application.ports.ItemTypeRepository;
import fr.huiitre.tools.modules.dofus.itemtype.domain.ItemType;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncItemTypesUseCase implements SecuredUseCase {

        private final ItemTypeRepository itemTypeRepository;
        private final List<ItemTypeDataProvider> itemTypeDataProviders;

        @Override
        public Optional<ModuleCode> requiredModule() {
                return Optional.of(ModuleCode.DOFUS);
        }

        @Override
        public RoleCode requiredRole() {
                return RoleCode.TECH;
        }

        public SyncItemTypesUseCase(
                        ItemTypeRepository itemTypeRepository,
                        List<ItemTypeDataProvider> itemTypeDataProviders) {
                this.itemTypeRepository = itemTypeRepository;
                this.itemTypeDataProviders = itemTypeDataProviders;
        }

        public SyncReport execute(GameVersionData gameVersion) {

                ItemTypeDataProvider itemTypeDataProvider = itemTypeDataProviders.stream()
                                .filter(p -> p.supports(gameVersion.getCode()))
                                .findFirst()
                                .orElseThrow(() -> new IllegalArgumentException("No ItemTypeDataProvider found for version: " + gameVersion.getCode()));

                List<ItemTypeSyncData> external = itemTypeDataProvider.fetchAll();

                List<ItemType> current = itemTypeRepository.findAllByGameVersionId(gameVersion.getId());

                Map<Long, ItemType> currentByAssetId = current.stream().collect(Collectors.toMap(
                                ItemType::getAssetId,
                                it -> it));

                List<String> created = new ArrayList<>();
                List<String> updated = new ArrayList<>();

                for (ItemTypeSyncData ext : external) {

                        ItemType existing = currentByAssetId.get(ext.getAssetId());

                        if (existing == null) {
                                ItemType createdItem = ItemType.create(
                                                ext.getAssetId(),
                                                gameVersion.getId(),
                                                ext.getCategoryId(),
                                                ext.getName());

                                itemTypeRepository.save(createdItem);

                                created.add(
                                                "assetId=" + ext.getAssetId()
                                                                + " name=\"" + ext.getName() + "\""
                                                                + " categoryId=" + ext.getCategoryId());
                                continue;
                        }

                        boolean nameChanged = !existing.getName().equals(ext.getName());
                        boolean categoryChanged = !existing.getCategoryId().equals(ext.getCategoryId());

                        if (nameChanged || categoryChanged) {

                                String oldName = existing.getName();
                                Long oldCategoryId = existing.getCategoryId();

                                existing.update(
                                                ext.getName(),
                                                ext.getCategoryId());

                                itemTypeRepository.update(existing);

                                updated.add(
                                                "assetId=" + ext.getAssetId()
                                                                + (nameChanged
                                                                                ? " name=\"" + oldName + "\" -> \""
                                                                                                + ext.getName() + "\""
                                                                                : "")
                                                                + (categoryChanged
                                                                                ? " categoryId=" + oldCategoryId
                                                                                                + " -> "
                                                                                                + ext.getCategoryId()
                                                                                : ""));
                        }
                }

                return new SyncReport(
                                "item_types",
                                "Item types",
                                created,
                                updated);
        }
}