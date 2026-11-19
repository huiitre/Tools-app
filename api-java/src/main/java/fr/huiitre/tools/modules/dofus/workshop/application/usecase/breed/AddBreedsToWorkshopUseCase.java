package fr.huiitre.tools.modules.dofus.workshop.application.usecase.breed;

import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.breed.application.ports.BreedRepository;
import fr.huiitre.tools.modules.dofus.breed.domain.Breed;
import fr.huiitre.tools.modules.dofus.breed.domain.BreedSex;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopDtoAssembler;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopBreedSex;

@Service
@Transactional
public class AddBreedsToWorkshopUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
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

    public AddBreedsToWorkshopUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository,
        BreedRepository breedRepository,
        WorkshopDtoAssembler workshopDtoAssembler
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.breedRepository = breedRepository;
        this.workshopDtoAssembler = workshopDtoAssembler;
    }

    public WorkshopDto execute(Long workshopId, Long gameVersionId, List<Long> breedIds) {
        Long userId = authenticatedUserProvider.getUserId();

        Workshop workshop = workshopRepository.findByIdAndUserId(userId, workshopId)
            .orElseThrow(() -> new WorkshopNotFoundException());

        //* Les classes d'une autre version du jeu sont ignorées, comme un tag inconnu.
        Map<Long, BreedSex> sexByBreedId = breedRepository.findAllByGameVersionId(gameVersionId).stream()
            .filter(breed -> breedIds.contains(breed.getId()))
            .collect(Collectors.toMap(Breed::getId, breed -> WorkshopBreedSex.of(breed.getAssetId())));

        workshopRepository.addBreedsToWorkshop(userId, workshopId, gameVersionId, sexByBreedId);

        return workshopDtoAssembler.toDto(userId, workshop);
    }
}
