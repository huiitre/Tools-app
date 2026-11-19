package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.service.WorkshopDtoAssembler;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

@Service
@Transactional
public class AddTagsToWorkshopUseCase implements SecuredUseCase {

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

    public AddTagsToWorkshopUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository,
        WorkshopDtoAssembler workshopDtoAssembler
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.workshopDtoAssembler = workshopDtoAssembler;
    }

    public WorkshopDto execute(Long workshopId, List<Long> tagIds) {
        Long userId = authenticatedUserProvider.getUserId();

        Workshop workshop = workshopRepository.findByIdAndUserId(userId, workshopId)
            .orElseThrow(() -> new WorkshopNotFoundException());

        workshopRepository.addTagsToWorkshop(userId, workshopId, tagIds);

        return workshopDtoAssembler.toDto(userId, workshop);
    }
}