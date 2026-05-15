package fr.huiitre.tools.modules.riot.sync.application;

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
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

@Service
@Transactional
public class SyncValorantSkinsUseCase implements SecuredUseCase {

    private final ValorantSkinDataProvider skinDataProvider;
    private final ValorantSkinSyncRepository skinSyncRepository;
    private final ValorantSkinLevelSyncRepository levelSyncRepository;
    private final ValorantSkinChromaSyncRepository chromaSyncRepository;

    public SyncValorantSkinsUseCase(
            ValorantSkinDataProvider skinDataProvider,
            ValorantSkinSyncRepository skinSyncRepository,
            ValorantSkinLevelSyncRepository levelSyncRepository,
            ValorantSkinChromaSyncRepository chromaSyncRepository) {
        this.skinDataProvider = skinDataProvider;
        this.skinSyncRepository = skinSyncRepository;
        this.levelSyncRepository = levelSyncRepository;
        this.chromaSyncRepository = chromaSyncRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public ValorantSyncReport execute(Map<UUID, Long> weaponAssetIdToDbId) {
        List<ValorantSkinSyncData> external = skinDataProvider.fetchAll();

        Map<UUID, ValorantSkinView> currentByAssetId = skinSyncRepository.findAll().stream()
                .collect(Collectors.toMap(ValorantSkinView::assetId, it -> it));

        Set<UUID> externalAssetIds = external.stream()
                .map(ValorantSkinSyncData::getAssetId)
                .collect(Collectors.toSet());

        Map<UUID, Long> skinAssetIdToDbId = new HashMap<>();
        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (ValorantSkinSyncData ext : external) {
            Long weaponId = weaponAssetIdToDbId.get(ext.getWeaponAssetId());
            ValorantSkinView existing = currentByAssetId.get(ext.getAssetId());

            if (existing == null) {
                Long newId = skinSyncRepository.save(ext, weaponId);
                skinAssetIdToDbId.put(ext.getAssetId(), newId);
                created++;
                continue;
            }

            skinAssetIdToDbId.put(ext.getAssetId(), existing.id());

            boolean changed = !Objects.equals(existing.name(), ext.getName())
                    || !Objects.equals(existing.iconUrl(), ext.getIconUrl())
                    || !Objects.equals(existing.tierUuid(), ext.getTierUuid())
                    || !Objects.equals(existing.contentTierUuid(), ext.getContentTierUuid())
                    || !Objects.equals(existing.weaponId(), weaponId);

            if (changed) {
                skinSyncRepository.update(existing.id(), ext, weaponId);
                updated++;
            }
        }

        for (ValorantSkinView current : currentByAssetId.values()) {
            if (!externalAssetIds.contains(current.assetId())) {
                skinSyncRepository.delete(current.id());
                deleted++;
            }
        }

        syncLevels(external, skinAssetIdToDbId);
        syncChromas(external, skinAssetIdToDbId);

        return new ValorantSyncReport(created, updated, deleted);
    }

    private void syncLevels(List<ValorantSkinSyncData> external, Map<UUID, Long> skinAssetIdToDbId) {
        levelSyncRepository.deleteAll();

        for (ValorantSkinSyncData skin : external) {
            Long skinId = skinAssetIdToDbId.get(skin.getAssetId());
            if (skinId == null) continue;

            for (ValorantSkinLevelSyncData level : skin.getLevels()) {
                levelSyncRepository.save(skinId, level);
            }
        }
    }

    private void syncChromas(List<ValorantSkinSyncData> external, Map<UUID, Long> skinAssetIdToDbId) {
        chromaSyncRepository.deleteAll();

        for (ValorantSkinSyncData skin : external) {
            Long skinId = skinAssetIdToDbId.get(skin.getAssetId());
            if (skinId == null) continue;

            for (ValorantSkinChromaSyncData chroma : skin.getChromas()) {
                chromaSyncRepository.save(skinId, chroma);
            }
        }
    }
}
