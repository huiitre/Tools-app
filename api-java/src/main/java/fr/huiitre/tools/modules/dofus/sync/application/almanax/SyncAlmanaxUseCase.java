package fr.huiitre.tools.modules.dofus.sync.application.almanax;

import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
import java.util.stream.Collectors;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.logging.infrastructure.DebugLogger;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxRepository;
import fr.huiitre.tools.modules.dofus.almanax.domain.Almanax;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.domain.Item;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;

@Service
@Transactional
public class SyncAlmanaxUseCase implements SecuredUseCase {

        private final AuthenticatedUserProvider authenticatedUserProvider;
        private final AlmanaxDataProvider almanaxDataProvider;
        private final AlmanaxRepository almanaxRepository;
        private final ItemRepository itemRepository;

        private static final DebugLogger log = DebugLogger.of(SyncAlmanaxUseCase.class);
        private static final Logger logger = LoggerFactory.getLogger(SyncAlmanaxUseCase.class);

        @Override
        public Optional<ModuleCode> requiredModule() {
                return Optional.of(ModuleCode.DOFUS);
        }

        @Override
        public RoleCode requiredRole() {
                return RoleCode.TECH;
        }

        public SyncAlmanaxUseCase(
                        AuthenticatedUserProvider authenticatedUserProvider,
                        AlmanaxDataProvider almanaxDataProvider,
                        AlmanaxRepository almanaxRepository,
                        ItemRepository itemRepository) {
                this.authenticatedUserProvider = authenticatedUserProvider;
                this.almanaxDataProvider = almanaxDataProvider;
                this.almanaxRepository = almanaxRepository;
                this.itemRepository = itemRepository;
        }

        public SyncReport execute(GameVersionData gameVersion) {

                List<AlmanaxSyncData> external = almanaxDataProvider.fetchAll();

                Map<Long, Long> assetsIdCounts = external.stream()
                                .collect(Collectors.groupingBy(AlmanaxSyncData::getAssetId, Collectors.counting()));

                assetsIdCounts.entrySet().stream()
                                .filter(entry -> entry.getValue() > 1)
                                .forEach(entry -> logger.warn("Asset ID {} is duplicated {} times", entry.getKey(),
                                                entry.getValue()));

                List<Almanax> current = almanaxRepository.findAll();

                Map<Long, Almanax> currentByAssetId = current.stream()
                                .collect(Collectors.toMap(Almanax::getAssetId, item -> item));

                List<String> created = new java.util.ArrayList<>();
                List<String> updated = new java.util.ArrayList<>();

                for (AlmanaxSyncData externalAlmanax : external) {

                        Long resolvedItemId = null;

                        if (externalAlmanax.getItemId() != null) {
                                resolvedItemId = itemRepository
                                                .findByAssetId(externalAlmanax.getItemId(), gameVersion.getId())
                                                .map(Item::getId)
                                                .orElse(null);
                        }

                        Almanax existing = currentByAssetId.get(externalAlmanax.getAssetId());

                        if (existing == null) {
                                Almanax newAlmanax = Almanax.create(
                                                externalAlmanax.getAssetId(),
                                                externalAlmanax.getName(),
                                                externalAlmanax.getDescription(),
                                                externalAlmanax.getDates(),
                                                resolvedItemId,
                                                externalAlmanax.getItemQuantity());
                                Long saved = almanaxRepository.save(newAlmanax);
                                created.add("""
                                                assetId=%d name=%s description=%s dates=%s id=%d
                                                """.formatted(
                                                externalAlmanax.getAssetId(),
                                                externalAlmanax.getName(),
                                                externalAlmanax.getDescription(),
                                                externalAlmanax.getDates().toString(),
                                                resolvedItemId,
                                                externalAlmanax.getItemQuantity(),
                                                saved));
                                continue;
                        }

                        boolean nameChanged = !existing.getName().equals(externalAlmanax.getName());
                        boolean descriptionChanged = !existing.getDescription()
                                        .equals(externalAlmanax.getDescription());
                        boolean datesChanged = !existing.getDates().equals(externalAlmanax.getDates());
                        boolean itemIdChanged = !Objects.equals(existing.getItemId(), resolvedItemId);
                        boolean itemQuantityChanged = !existing.getItemQuantity()
                                        .equals(externalAlmanax.getItemQuantity());

                        if (nameChanged || descriptionChanged || datesChanged || itemIdChanged || itemQuantityChanged) {

                                String oldName = existing.getName();
                                String oldDescription = existing.getDescription();
                                List<String> oldDates = existing.getDates();
                                Long oldItemId = existing.getItemId();
                                Long oldItemQuantity = existing.getItemQuantity();

                                existing.update(
                                                externalAlmanax.getName(),
                                                externalAlmanax.getDescription(),
                                                externalAlmanax.getDates(),
                                                resolvedItemId,
                                                externalAlmanax.getItemQuantity());

                                almanaxRepository.update(existing);
                                updated.add("""
                                                id : %d
                                                assetId : %d
                                                name : %s -> %s
                                                description : %s -> %s
                                                dates : %s -> %s
                                                itemId : %d -> %d
                                                itemQuantity : %d -> %d
                                                """.formatted(
                                                existing.getId(),
                                                externalAlmanax.getAssetId(),
                                                oldName,
                                                externalAlmanax.getName(),
                                                oldDescription,
                                                externalAlmanax.getDescription(),
                                                oldDates.toString(),
                                                externalAlmanax.getDates().toString(),
                                                oldItemId,
                                                resolvedItemId,
                                                oldItemQuantity,
                                                externalAlmanax.getItemQuantity()));
                        }
                }

                return new SyncReport(
                                "almanax",
                                "Almanax",
                                created,
                                updated);
        }
}