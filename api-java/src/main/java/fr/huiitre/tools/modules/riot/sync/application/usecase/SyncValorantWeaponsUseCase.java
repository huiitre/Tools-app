package fr.huiitre.tools.modules.riot.sync.application.usecase;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSyncReport;
import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponSyncResult;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantWeaponDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantWeaponSyncRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantWeaponView;

@Service
@Transactional
public class SyncValorantWeaponsUseCase implements SecuredUseCase {

    private final ValorantWeaponDataProvider weaponDataProvider;
    private final ValorantWeaponSyncRepository weaponSyncRepository;

    public SyncValorantWeaponsUseCase(
            ValorantWeaponDataProvider weaponDataProvider,
            ValorantWeaponSyncRepository weaponSyncRepository) {
        this.weaponDataProvider = weaponDataProvider;
        this.weaponSyncRepository = weaponSyncRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public ValorantWeaponSyncResult execute() {
        List<ValorantWeaponSyncData> external = weaponDataProvider.fetchAll();

        Map<UUID, ValorantWeaponView> currentByAssetId = weaponSyncRepository.findAll().stream()
                .collect(Collectors.toMap(ValorantWeaponView::assetId, it -> it));

        Set<UUID> externalAssetIds = external.stream()
                .map(ValorantWeaponSyncData::getAssetId)
                .collect(Collectors.toSet());

        Map<UUID, Long> weaponAssetIdToDbId = new HashMap<>();
        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (ValorantWeaponSyncData ext : external) {
            ValorantWeaponView existing = currentByAssetId.get(ext.getAssetId());

            if (existing == null) {
                Long newId = weaponSyncRepository.save(ext);
                weaponAssetIdToDbId.put(ext.getAssetId(), newId);
                created++;
                continue;
            }

            weaponAssetIdToDbId.put(ext.getAssetId(), existing.id());

            boolean changed = !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.category(), ext.getCategory())
                    || !Objects.equals(existing.defaultSkinAssetId(), ext.getDefaultSkinAssetId())
                    || !Objects.equals(existing.displayIconUrl(), ext.getDisplayIconUrl());

            if (changed) {
                weaponSyncRepository.update(existing.id(), ext);
                updated++;
            }
        }

        for (ValorantWeaponView current : currentByAssetId.values()) {
            if (!externalAssetIds.contains(current.assetId())) {
                weaponSyncRepository.delete(current.id());
                deleted++;
            }
        }

        return new ValorantWeaponSyncResult(new ValorantSyncReport(created, updated, deleted), weaponAssetIdToDbId);
    }
}
