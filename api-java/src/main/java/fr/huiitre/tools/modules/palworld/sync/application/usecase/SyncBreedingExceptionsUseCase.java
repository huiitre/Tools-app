package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.HashSet;
import java.util.List;
import java.util.Map;
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
import fr.huiitre.tools.modules.palworld.sync.application.BreedingExceptionSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.BreedingExceptionSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.ports.BreedingExceptionDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.BreedingExceptionSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalSyncRepository;
import fr.huiitre.tools.modules.palworld.sync.application.view.PalRefView;

@Service
@Transactional
public class SyncBreedingExceptionsUseCase implements SecuredUseCase {

    private static final Logger log = LoggerFactory.getLogger(SyncBreedingExceptionsUseCase.class);

    private final BreedingExceptionDataProvider dataProvider;
    private final BreedingExceptionSyncRepository syncRepository;
    private final PalSyncRepository palSyncRepository;

    public SyncBreedingExceptionsUseCase(
            BreedingExceptionDataProvider dataProvider,
            BreedingExceptionSyncRepository syncRepository,
            PalSyncRepository palSyncRepository) {
        this.dataProvider = dataProvider;
        this.syncRepository = syncRepository;
        this.palSyncRepository = palSyncRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public BreedingExceptionSyncReport execute() {
        List<BreedingExceptionSyncData> external = dataProvider.fetchAll();

        Map<String, Long> palIdByTribeUpper = palSyncRepository.findAll().stream()
                .collect(Collectors.toMap(pal -> pal.tribe().toUpperCase(), PalRefView::id, (a, b) -> a));

        syncRepository.deleteAll();

        // Le jeu réel de breeding.json contient des lignes qui référencent des espèces absentes du pak
        // (ex: WindChimes/WindChimes_Ice, sans équivalent pak connu) et au moins un doublon exact
        // (même paire+genres, même enfant) : les deux cas sont ignorés+logués plutôt que de faire échouer
        // tout le sync pour 258 lignes de données externes.
        Set<String> seen = new HashSet<>();
        int inserted = 0;
        int skipped = 0;

        for (BreedingExceptionSyncData e : external) {
            Long parentAId = palIdByTribeUpper.get(upper(e.parentATribe()));
            Long parentBId = palIdByTribeUpper.get(upper(e.parentBTribe()));
            Long childId = palIdByTribeUpper.get(upper(e.childTribe()));

            if (parentAId == null || parentBId == null || childId == null) {
                log.warn("Palworld breeding exception ignorée, espèce introuvable dans le catalogue: {}", e);
                skipped++;
                continue;
            }

            String key = parentAId + "|" + e.parentAGenderCode() + "|" + parentBId + "|" + e.parentBGenderCode();
            if (!seen.add(key)) {
                skipped++;
                continue;
            }

            if (syncRepository.insert(parentAId, e.parentAGenderCode(), parentBId, e.parentBGenderCode(), childId)) {
                inserted++;
            } else {
                skipped++;
            }
        }

        log.info("Palworld breeding exceptions sync: {} inserted, {} skipped", inserted, skipped);
        return new BreedingExceptionSyncReport(inserted, skipped);
    }

    private String upper(String tribe) {
        return tribe == null ? null : tribe.toUpperCase();
    }
}
