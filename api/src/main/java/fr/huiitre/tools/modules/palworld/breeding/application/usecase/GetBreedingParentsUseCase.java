package fr.huiitre.tools.modules.palworld.breeding.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.ports.BreedingCatalogRepository;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingParentPairView;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingException;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingIndexBuilder;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPairResult;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;

@Service
public class GetBreedingParentsUseCase implements SecuredUseCase {

    private final BreedingCatalogRepository breedingCatalogRepository;

    public GetBreedingParentsUseCase(BreedingCatalogRepository breedingCatalogRepository) {
        this.breedingCatalogRepository = breedingCatalogRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    // Recalculé à la demande à chaque appel (pas de cache) : ~48k paires pour 309 espèces, de l'ordre de
    // la dizaine de ms en JVM (cf. BreedingEngineTest#should_keep_direct_computation_and_reverse_index_consistent
    // qui refait ce calcul en entier) — inutile de précalculer/stocker un index, ça alourdirait juste le
    // démarrage et introduirait un état à invalider pour rien.
    public List<BreedingParentPairView> execute(Long childPalId) {
        List<BreedingPal> allPals = breedingCatalogRepository.findAllPals();
        List<BreedingException> exceptions = breedingCatalogRepository.findAllExceptions();
        Map<Long, BreedingPal> byId = allPals.stream().collect(Collectors.toMap(BreedingPal::id, pal -> pal));

        List<BreedingPairResult> pairs = BreedingIndexBuilder.buildAll(allPals, exceptions).stream()
                .filter(pair -> pair.childPalId().equals(childPalId))
                .toList();

        return pairs.stream()
                .map(pair -> BreedingParentPairView.from(byId.get(pair.parentAPalId()), byId.get(pair.parentBPalId()), pair))
                .toList();
    }
}
