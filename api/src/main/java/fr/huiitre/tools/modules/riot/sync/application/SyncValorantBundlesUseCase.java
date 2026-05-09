package fr.huiitre.tools.modules.riot.sync.application;

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
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantBundleView;

@Service
@Transactional
public class SyncValorantBundlesUseCase implements SecuredUseCase {

    private final ValorantBundleDataProvider bundleDataProvider;
    private final ValorantBundleSyncRepository bundleSyncRepository;

    public SyncValorantBundlesUseCase(
            ValorantBundleDataProvider bundleDataProvider,
            ValorantBundleSyncRepository bundleSyncRepository) {
        this.bundleDataProvider = bundleDataProvider;
        this.bundleSyncRepository = bundleSyncRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public ValorantSyncReport execute() {
        Map<UUID, ValorantBundleView> currentByAssetId = bundleSyncRepository.findAll().stream()
                .collect(Collectors.toMap(ValorantBundleView::assetId, it -> it));

        Set<UUID> externalAssetIds = bundleDataProvider.fetchAll().stream()
                .map(ValorantBundleSyncData::getAssetId)
                .collect(Collectors.toSet());

        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (ValorantBundleSyncData ext : bundleDataProvider.fetchAll()) {
            ValorantBundleView existing = currentByAssetId.get(ext.getAssetId());

            if (existing == null) {
                bundleSyncRepository.save(ext);
                created++;
                continue;
            }

            boolean changed = !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.bannerUrl(), ext.getBannerUrl());

            if (changed) {
                bundleSyncRepository.update(existing.id(), ext);
                updated++;
            }
        }

        for (ValorantBundleView current : currentByAssetId.values()) {
            if (!externalAssetIds.contains(current.assetId())) {
                bundleSyncRepository.delete(current.id());
                deleted++;
            }
        }

        return new ValorantSyncReport(created, updated, deleted);
    }
}
