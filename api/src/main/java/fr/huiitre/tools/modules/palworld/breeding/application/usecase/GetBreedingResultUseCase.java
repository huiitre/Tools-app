package fr.huiitre.tools.modules.palworld.breeding.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.exception.BreedingSpeciesNotFoundException;
import fr.huiitre.tools.modules.palworld.breeding.application.ports.BreedingCatalogRepository;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingResultView;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingComputation;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingEngine;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingException;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;
import fr.huiitre.tools.modules.palworld.domain.breeding.Gender;

@Service
public class GetBreedingResultUseCase implements SecuredUseCase {

    private final BreedingCatalogRepository breedingCatalogRepository;

    public GetBreedingResultUseCase(BreedingCatalogRepository breedingCatalogRepository) {
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

    public BreedingResultView execute(Long parentAId, String genderACode, Long parentBId, String genderBCode) {
        List<BreedingPal> allPals = breedingCatalogRepository.findAllPals();
        Map<Long, BreedingPal> byId = allPals.stream().collect(Collectors.toMap(BreedingPal::id, pal -> pal));

        BreedingPal parentA = byId.get(parentAId);
        BreedingPal parentB = byId.get(parentBId);
        if (parentA == null) throw new BreedingSpeciesNotFoundException(parentAId);
        if (parentB == null) throw new BreedingSpeciesNotFoundException(parentBId);

        List<BreedingException> exceptions = breedingCatalogRepository.findAllExceptions();
        BreedingComputation computation = BreedingEngine.compute(
                parentA, Gender.fromCode(genderACode), parentB, Gender.fromCode(genderBCode), exceptions, allPals);

        BreedingPal child = byId.get(computation.childPalId());
        return BreedingResultView.from(parentA, parentB, child, computation);
    }
}
