package fr.huiitre.tools.modules.palworld.breeding.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.exception.BreedingSpeciesNotFoundException;
import fr.huiitre.tools.modules.palworld.breeding.application.ports.BreedingCatalogRepository;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingPathNodeView;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingPathResultView;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingPathStepView;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingSpeciesRefView;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingException;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingIndexBuilder;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPairResult;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPal;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPathBuilder;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingPathNode;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingRule;
import fr.huiitre.tools.modules.palworld.domain.breeding.Gender;

@Service
public class GetBreedingPathUseCase implements SecuredUseCase {

    private final BreedingCatalogRepository breedingCatalogRepository;

    public GetBreedingPathUseCase(BreedingCatalogRepository breedingCatalogRepository) {
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

    public BreedingPathResultView execute(Long targetSpeciesId, Set<Long> ownedSpeciesIds) {
        List<BreedingPal> allPals = breedingCatalogRepository.findAllPals();
        Map<Long, BreedingPal> byId = allPals.stream().collect(Collectors.toMap(BreedingPal::id, pal -> pal));
        if (!byId.containsKey(targetSpeciesId)) throw new BreedingSpeciesNotFoundException(targetSpeciesId);

        List<BreedingException> exceptions = breedingCatalogRepository.findAllExceptions();
        List<BreedingPairResult> allPairs = BreedingIndexBuilder.buildAll(allPals, exceptions);

        Optional<BreedingPathNode> path = BreedingPathBuilder.build(targetSpeciesId, ownedSpeciesIds, allPairs);
        if (path.isEmpty()) return new BreedingPathResultView(false, null);

        return new BreedingPathResultView(true, toView(path.get(), byId));
    }

    private BreedingPathNodeView toView(BreedingPathNode node, Map<Long, BreedingPal> byId) {
        BreedingSpeciesRefView species = BreedingSpeciesRefView.from(byId.get(node.speciesId()));
        if (node.owned()) return new BreedingPathNodeView(species, true, null);

        BreedingPairResult pair = node.step().pair();
        BreedingPathStepView step = new BreedingPathStepView(
                toView(node.step().parentA(), byId),
                toView(node.step().parentB(), byId),
                genderCode(pair.parentAGender()),
                genderCode(pair.parentBGender()),
                pair.rule() == BreedingRule.EXCEPTION ? "exception" : "formula");
        return new BreedingPathNodeView(species, false, step);
    }

    private String genderCode(Gender gender) {
        return gender == null ? null : gender.toCode();
    }
}
