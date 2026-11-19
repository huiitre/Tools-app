package fr.huiitre.tools.modules.dofus.workshop.application.usecase.job;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopDtoAssembler;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;

@Service
@Transactional
public class AddJobsToWorkshopUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
    private final WorkshopDtoAssembler workshopDtoAssembler;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public AddJobsToWorkshopUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository,
        WorkshopDtoAssembler workshopDtoAssembler
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.workshopDtoAssembler = workshopDtoAssembler;
    }

    public WorkshopDto execute(Long workshopId, Long gameVersionId, List<Long> jobIds) {
        Long userId = authenticatedUserProvider.getUserId();

        Workshop workshop = workshopRepository.findByIdAndUserId(userId, workshopId)
            .orElseThrow(() -> new WorkshopNotFoundException());

        workshopRepository.addJobsToWorkshop(userId, workshopId, gameVersionId, jobIds);

        return workshopDtoAssembler.toDto(userId, workshop);
    }
}
