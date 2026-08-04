package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
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
import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.PalRefView;

@Service
@Transactional
public class SyncPalsUseCase implements SecuredUseCase {

    private static final Logger log = LoggerFactory.getLogger(SyncPalsUseCase.class);

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
            Map<String, Long> elementIdByName,
            Map<String, Long> workSuitabilityIdBySlug,
            Map<String, Long> skillIdByName) {
        List<PalSyncData> external = dataProvider.fetchAll();

        // Matching insensible à la casse : cf. tools_palworld.pal_instance qui résout déjà character_id -> pal
        // via findIdByTribeUpper() (PostgresPalLookupRepository). "BluePlatypus" (pak) vs "Blueplatypus" (ancien
        // scraper) sont le même Pal, seule la casse diffère d'une source à l'autre.
        Map<String, PalRefView> currentByTribeUpper = syncRepository.findAll().stream()
                .collect(Collectors.toMap(pal -> pal.tribe().toUpperCase(), it -> it));

        Set<String> externalTribesUpper = external.stream()
                .map(pal -> pal.getTribe().toUpperCase())
                .collect(Collectors.toSet());

        Map<String, Long> palIdByTribeUpper = new HashMap<>();
        int created = 0;
        int updated = 0;
        int deleted = 0;

        for (PalSyncData ext : external) {
            String tribeUpper = ext.getTribe().toUpperCase();
            PalRefView existing = currentByTribeUpper.get(tribeUpper);

            if (existing == null) {
                Long newId = syncRepository.save(ext);
                palIdByTribeUpper.put(tribeUpper, newId);
                created++;
            } else {
                palIdByTribeUpper.put(tribeUpper, existing.id());

                if (hasChanged(existing, ext)) {
                    syncRepository.update(existing.id(), ext);
                    updated++;
                }
            }

            syncRepository.upsertSource(palIdByTribeUpper.get(tribeUpper), ext.getSourceSlug(), ext.getSourceUrl(),
                    ext.getRawPayloadJson(), ext.getFetchedAt());
        }

        List<String> deletedTribes = new ArrayList<>();
        for (PalRefView current : currentByTribeUpper.values()) {
            if (!externalTribesUpper.contains(current.tribe().toUpperCase())) {
                syncRepository.delete(current.id());
                deletedTribes.add(current.tribe());
                deleted++;
            }
        }

        // Garde-fou : si la clé tribe (id pak vs tribe ex-scraper) a changé de format pour une part significative
        // des Pals, cette liste explose au lieu de rester proche de 0 — signal à vérifier avant de faire confiance
        // au sync (perte silencieuse d'image_url/description "sticky" pour tout Pal vu comme supprimé+recréé).
        List<String> createdTribes = external.stream()
                .map(PalSyncData::getTribe)
                .filter(tribe -> !currentByTribeUpper.containsKey(tribe.toUpperCase()))
                .toList();
        log.info("Palworld pals sync: {} created {}, {} deleted {}", created, createdTribes, deleted, deletedTribes);

        replaceChildren(external, palIdByTribeUpper, elementIdByName, workSuitabilityIdBySlug, skillIdByName);

        return new PalworldSyncReport(created, updated, deleted);
    }

    private boolean hasChanged(PalRefView existing, PalSyncData ext) {
        return !Objects.equals(existing.paldexIndex(), ext.getPaldexIndex())
                || !Objects.equals(existing.name(), ext.getName())
                || !Objects.equals(existing.size(), ext.getSize())
                || !Objects.equals(existing.rarity(), ext.getRarity())
                || !Objects.equals(existing.baseHp(), ext.getBaseHp())
                || !Objects.equals(existing.baseAttack(), ext.getBaseAttack())
                || !Objects.equals(existing.baseDefense(), ext.getBaseDefense())
                || !Objects.equals(existing.baseWorkSpeed(), ext.getBaseWorkSpeed())
                || !Objects.equals(existing.baseSupport(), ext.getBaseSupport())
                || !Objects.equals(existing.runSpeed(), ext.getRunSpeed())
                || !Objects.equals(existing.rideSprintSpeed(), ext.getRideSprintSpeed())
                || !Objects.equals(existing.captureRateCorrect(), ext.getCaptureRateCorrect())
                || !Objects.equals(existing.maleProbability(), ext.getMaleProbability())
                || !Objects.equals(existing.combiRank(), ext.getCombiRank())
                || !Objects.equals(existing.price(), ext.getPrice())
                || !Objects.equals(existing.bestWorkSuitabilityLabel(), ext.getBestWorkSuitabilityLabel())
                || !Objects.equals(existing.imageUrl(), ext.getImageUrl())
                || !Objects.equals(existing.description(), ext.getDescription());
    }

    private void replaceChildren(
            List<PalSyncData> external,
            Map<String, Long> palIdByTribeUpper,
            Map<String, Long> elementIdByName,
            Map<String, Long> workSuitabilityIdBySlug,
            Map<String, Long> skillIdByName) {
        syncRepository.deleteAllChildren();

        for (PalSyncData pal : external) {
            Long palId = palIdByTribeUpper.get(pal.getTribe().toUpperCase());
            if (palId == null) continue;

            syncRepository.saveElements(palId, pal, elementIdByName);
            syncRepository.saveWorkSuitabilities(palId, pal, workSuitabilityIdBySlug);
            syncRepository.saveActiveSkills(palId, pal, skillIdByName);
            syncRepository.savePassiveSkills(palId, pal);
            syncRepository.saveDrops(palId, pal);
        }
    }
}
