package fr.huiitre.tools.modules.riot.sync.application.usecase;

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
import fr.huiitre.tools.modules.riot.sync.application.ValorantContentTierSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSyncReport;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantContentTierDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantContentTierSyncRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantContentTierView;

@Service
@Transactional
public class SyncValorantContentTiersUseCase implements SecuredUseCase {

    private final ValorantContentTierDataProvider contentTierDataProvider;
    private final ValorantContentTierSyncRepository contentTierSyncRepository;

    public SyncValorantContentTiersUseCase(
            ValorantContentTierDataProvider contentTierDataProvider,
            ValorantContentTierSyncRepository contentTierSyncRepository) {
        this.contentTierDataProvider = contentTierDataProvider;
        this.contentTierSyncRepository = contentTierSyncRepository;
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
        Map<UUID, ValorantContentTierView> currentByAssetId = contentTierSyncRepository.findAll().stream()
                .collect(Collectors.toMap(ValorantContentTierView::assetId, it -> it));

        List<ValorantContentTierSyncData> external = contentTierDataProvider.fetchAll();

        Set<UUID> externalAssetIds = external.stream()
                .map(ValorantContentTierSyncData::getAssetId)
                .collect(Collectors.toSet());

        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (ValorantContentTierSyncData ext : external) {
            ValorantContentTierView existing = currentByAssetId.get(ext.getAssetId());

            if (existing == null) {
                contentTierSyncRepository.save(ext);
                created++;
                continue;
            }

            boolean changed = !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.devName(), ext.getDevName())
                    || existing.rank() != ext.getRank()
                    || existing.juiceValue() != ext.getJuiceValue()
                    || existing.juiceCost() != ext.getJuiceCost()
                    || !Objects.equals(existing.highlightColor(), ext.getHighlightColor())
                    || !Objects.equals(existing.displayIconUrl(), ext.getDisplayIconUrl());

            if (changed) {
                contentTierSyncRepository.update(existing.id(), ext);
                updated++;
            }
        }

        for (ValorantContentTierView current : currentByAssetId.values()) {
            if (!externalAssetIds.contains(current.assetId())) {
                contentTierSyncRepository.delete(current.id());
                deleted++;
            }
        }

        return new ValorantSyncReport(created, updated, deleted);
    }
}
