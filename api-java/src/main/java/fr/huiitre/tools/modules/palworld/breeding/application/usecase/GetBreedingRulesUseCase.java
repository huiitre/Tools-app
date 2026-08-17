package fr.huiitre.tools.modules.palworld.breeding.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.ports.BreedingCatalogRepository;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingRuleView;
import fr.huiitre.tools.modules.palworld.domain.breeding.BreedingException;

@Service
public class GetBreedingRulesUseCase implements SecuredUseCase {

    private final BreedingCatalogRepository breedingCatalogRepository;

    public GetBreedingRulesUseCase(BreedingCatalogRepository breedingCatalogRepository) {
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

    public List<BreedingRuleView> execute() {
        return breedingCatalogRepository.findAllExceptions().stream().map(this::toView).toList();
    }

    private BreedingRuleView toView(BreedingException exception) {
        return new BreedingRuleView(
                exception.parentAPalId(), genderCode(exception.parentAGender()),
                exception.parentBPalId(), genderCode(exception.parentBGender()),
                exception.childPalId());
    }

    private String genderCode(fr.huiitre.tools.modules.palworld.domain.breeding.Gender gender) {
        return gender == null ? null : gender.toCode();
    }
}
