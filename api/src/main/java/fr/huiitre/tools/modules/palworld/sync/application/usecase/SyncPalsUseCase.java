package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.PalRefView;

@Service
@Transactional
public class SyncPalsUseCase implements SecuredUseCase {

    private final PalDataProvider dataProvider;
    private final PalSyncRepository syncRepository;

    public SyncPalsUseCase(PalDataProvider dataProvider, PalSyncRepository syncRepository) {
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

    public PalworldSyncReport execute(
            Map<String, Long> elementIdByExternalCode,
            Map<String, Long> workSuitabilityIdBySlug,
            Map<String, Long> skillIdBySlug) {
        List<PalSyncData> external = dataProvider.fetchAll();

        Map<String, PalRefView> currentByTribe = syncRepository.findAll().stream()
                .collect(Collectors.toMap(PalRefView::tribe, it -> it));

        Set<String> externalTribes = external.stream()
                .map(PalSyncData::getTribe)
                .collect(Collectors.toSet());

        Map<String, Long> palIdByTribe = new HashMap<>();
        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (PalSyncData ext : external) {
            PalRefView existing = currentByTribe.get(ext.getTribe());

            if (existing == null) {
                Long newId = syncRepository.save(ext);
                palIdByTribe.put(ext.getTribe(), newId);
                created++;
            } else {
                palIdByTribe.put(ext.getTribe(), existing.id());

                if (hasChanged(existing, ext)) {
                    syncRepository.update(existing.id(), ext);
                    updated++;
                }
            }

            syncRepository.upsertSource(palIdByTribe.get(ext.getTribe()), ext.getSourceSlug(), ext.getSourceUrl(),
                    ext.getRawPayloadJson(), ext.getFetchedAt());
        }

        for (PalRefView current : currentByTribe.values()) {
            if (!externalTribes.contains(current.tribe())) {
                syncRepository.delete(current.id());
                deleted++;
            }
        }

        replaceChildren(external, palIdByTribe, elementIdByExternalCode, workSuitabilityIdBySlug, skillIdBySlug);

        return new PalworldSyncReport(created, updated, deleted);
    }

    private boolean hasChanged(PalRefView existing, PalSyncData ext) {
        return !Objects.equals(existing.paldexIndex(), ext.getPaldexIndex())
                || !Objects.equals(existing.paldexSuffix(), ext.getPaldexSuffix())
                || !Objects.equals(existing.name(), ext.getName())
                || !Objects.equals(existing.imageUrl(), ext.getImageUrl())
                || !Objects.equals(existing.description(), ext.getDescription())
                || !Objects.equals(existing.size(), ext.getSize())
                || !Objects.equals(existing.rarity(), ext.getRarity())
                || !Objects.equals(existing.baseHp(), ext.getBaseHp())
                || !Objects.equals(existing.baseAttack(), ext.getBaseAttack())
                || !Objects.equals(existing.baseDefense(), ext.getBaseDefense())
                || !Objects.equals(existing.baseWorkSpeed(), ext.getBaseWorkSpeed())
                || !Objects.equals(existing.baseSupport(), ext.getBaseSupport())
                || !Objects.equals(existing.foodAmount(), ext.getFoodAmount())
                || !Objects.equals(existing.captureRateCorrect(), ext.getCaptureRateCorrect())
                || !Objects.equals(existing.maleProbability(), ext.getMaleProbability())
                || !Objects.equals(existing.combiRank(), ext.getCombiRank())
                || !Objects.equals(existing.goldCoin(), ext.getGoldCoin())
                || !Objects.equals(existing.eggType(), ext.getEggType())
                || !Objects.equals(existing.bestWorkSuitabilityLabel(), ext.getBestWorkSuitabilityLabel());
    }

    private void replaceChildren(
            List<PalSyncData> external,
            Map<String, Long> palIdByTribe,
            Map<String, Long> elementIdByExternalCode,
            Map<String, Long> workSuitabilityIdBySlug,
            Map<String, Long> skillIdBySlug) {
        syncRepository.deleteAllChildren();

        for (PalSyncData pal : external) {
            Long palId = palIdByTribe.get(pal.getTribe());
            if (palId == null) continue;

            syncRepository.saveElements(palId, pal, elementIdByExternalCode);
            syncRepository.saveWorkSuitabilities(palId, pal, workSuitabilityIdBySlug);
            syncRepository.saveActiveSkills(palId, pal, skillIdBySlug);
            syncRepository.savePassiveSkills(palId, pal);
            syncRepository.savePartnerSkill(palId, pal);
            syncRepository.saveDrops(palId, pal);
            syncRepository.saveVariants(palId, pal);
            syncRepository.saveSpawnZones(palId, pal);
        }
    }
}
