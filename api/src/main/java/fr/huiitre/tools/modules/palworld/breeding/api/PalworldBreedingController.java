package fr.huiitre.tools.modules.palworld.breeding.api;

import java.util.List;
import java.util.Set;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.breeding.application.usecase.GetBreedingAsParentUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.usecase.GetBreedingParentsUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.usecase.GetBreedingPathUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.usecase.GetBreedingResultUseCase;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingCombinationView;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingPathResultView;
import fr.huiitre.tools.modules.palworld.breeding.application.view.BreedingResultView;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Palworld - Breeding")
@RestController
@RequestMapping("/palworld/breeding")
public class PalworldBreedingController {

    private final GetBreedingResultUseCase getBreedingResultUseCase;
    private final GetBreedingParentsUseCase getBreedingParentsUseCase;
    private final GetBreedingAsParentUseCase getBreedingAsParentUseCase;
    private final GetBreedingPathUseCase getBreedingPathUseCase;

    public PalworldBreedingController(
            GetBreedingResultUseCase getBreedingResultUseCase,
            GetBreedingParentsUseCase getBreedingParentsUseCase,
            GetBreedingAsParentUseCase getBreedingAsParentUseCase,
            GetBreedingPathUseCase getBreedingPathUseCase) {
        this.getBreedingResultUseCase = getBreedingResultUseCase;
        this.getBreedingParentsUseCase = getBreedingParentsUseCase;
        this.getBreedingAsParentUseCase = getBreedingAsParentUseCase;
        this.getBreedingPathUseCase = getBreedingPathUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/result")
    @ResponseStatus(HttpStatus.OK)
    public BreedingResultView getResult(
            @RequestParam Long parentA,
            @RequestParam Long parentB,
            @RequestParam(required = false) String genderA,
            @RequestParam(required = false) String genderB) {
        return getBreedingResultUseCase.execute(parentA, genderA, parentB, genderB);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/parents")
    @ResponseStatus(HttpStatus.OK)
    public List<BreedingCombinationView> getParents(@RequestParam("child") Long childId) {
        return getBreedingParentsUseCase.execute(childId);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/as-parent")
    @ResponseStatus(HttpStatus.OK)
    public List<BreedingCombinationView> getAsParent(@RequestParam("pal") Long palId) {
        return getBreedingAsParentUseCase.execute(palId);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/path")
    @ResponseStatus(HttpStatus.OK)
    public BreedingPathResultView getPath(@RequestParam Long target, @RequestParam Set<Long> owned) {
        return getBreedingPathUseCase.execute(target, owned);
    }
}
