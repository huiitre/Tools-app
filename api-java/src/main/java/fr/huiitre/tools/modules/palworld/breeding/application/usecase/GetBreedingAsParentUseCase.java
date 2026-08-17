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
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingCombinationView;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingException;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingIndexBuilder;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPairResult;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;

// Miroir de GetBreedingParentsUseCase : au lieu de filtrer l'index par enfant, on filtre par une espèce
// apparaissant comme parentA OU parentB — "toutes les combinaisons possibles en utilisant ce Pal comme
// parent". Même approche pas de cache (cf. commentaire de GetBreedingParentsUseCase).
@Service
public class GetBreedingAsParentUseCase implements SecuredUseCase {

    private final BreedingCatalogRepository breedingCatalogRepository;

    public GetBreedingAsParentUseCase(BreedingCatalogRepository breedingCatalogRepository) {
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

    public List<BreedingCombinationView> execute(Long parentPalId) {
        List<BreedingPal> allPals = breedingCatalogRepository.findAllPals();
        Map<Long, BreedingPal> byId = allPals.stream().collect(Collectors.toMap(BreedingPal::id, pal -> pal));
        List<BreedingException> exceptions = breedingCatalogRepository.findAllExceptions();

        List<BreedingPairResult> pairs = BreedingIndexBuilder.buildAll(allPals, exceptions).stream()
                .filter(pair -> pair.parentAPalId().equals(parentPalId) || pair.parentBPalId().equals(parentPalId))
                .toList();

        return pairs.stream()
                .map(pair -> BreedingCombinationView.from(
                        byId.get(pair.parentAPalId()), byId.get(pair.parentBPalId()), byId.get(pair.childPalId()), pair))
                .toList();
    }
}
