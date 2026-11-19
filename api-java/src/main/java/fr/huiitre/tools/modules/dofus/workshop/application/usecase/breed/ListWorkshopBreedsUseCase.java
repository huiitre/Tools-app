package fr.huiitre.tools.modules.dofus.workshop.application.usecase.breed;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.breed.application.ports.BreedRepository;
import fr.huiitre.tools.modules.dofus.breed.domain.Breed;
import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopBreedDto;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopDtoAssembler;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopBreedSex;

//* Classes proposables sur un atelier, avec la tête que l'atelier affichera.
@Service
@Transactional(readOnly = true)
public class ListWorkshopBreedsUseCase implements SecuredUseCase {

    private final BreedRepository breedRepository;
    private final WorkshopDtoAssembler workshopDtoAssembler;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public ListWorkshopBreedsUseCase(
        BreedRepository breedRepository,
        WorkshopDtoAssembler workshopDtoAssembler
    ) {
        this.breedRepository = breedRepository;
        this.workshopDtoAssembler = workshopDtoAssembler;
    }

    public List<WorkshopBreedDto> execute(Long gameVersionId) {
        Map<Long, Map<BreedSex, Long>> headsByBreedId = breedRepository.findHeadIconIdsByBreedId(gameVersionId);

        return breedRepository.findAllByGameVersionId(gameVersionId).stream()
            .sorted(Comparator.comparing(Breed::getName))
            .map(breed -> workshopDtoAssembler.breedDto(
                breed.getId(),
                breed.getName(),
                headsByBreedId.getOrDefault(breed.getId(), Map.of()).get(WorkshopBreedSex.of(breed.getAssetId()))))
            .toList();
    }
}
