package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopDtoAssembler;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopLink;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

@Service
@Transactional
public class ListWorkshopsUseCase implements SecuredUseCase {

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

    public ListWorkshopsUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository,
        WorkshopDtoAssembler workshopDtoAssembler
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.workshopDtoAssembler = workshopDtoAssembler;
    }

    public List<WorkshopDto> execute(Long gameVersionId) {
        
        Long userId = authenticatedUserProvider.getUserId();

        List<Workshop> workshops = workshopRepository.findAllByUserIdAndGameVersionId(userId, gameVersionId);

        Map<Long, List<WorkshopLink>> linksByWorkshopId = workshopRepository.findAllLinksByUserIdGroupedByWorkshopId(userId);

        return workshopDtoAssembler.toDtos(userId, workshops, linksByWorkshopId);
    }
}