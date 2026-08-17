package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.List;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.CreateWorkshopRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDto;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;

@Service
@Transactional
public class CreateWorkshopUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final Logger logger = LoggerFactory.getLogger(CreateWorkshopUseCase.class);

    private final WorkshopRepository workshopRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public CreateWorkshopUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
    }

    public WorkshopDto execute(Long gameVersionId, CreateWorkshopRequest request) {

        logger.debug("Ligne #45 || request.name() : {}", request.name());
        
        Long userId = authenticatedUserProvider.getUserId();

        boolean exists = workshopRepository.existsByUserIdAndName(userId, request.name());

        if (exists) throw new IllegalArgumentException("Un atelier avec ce nom existe déjà.");

        Workshop workshop = Workshop.create(
            request.name()
        );

        Long id = workshopRepository.create(
            gameVersionId,
            userId,
            workshop
        );

        return new WorkshopDto(
            id,
            workshop.getName(),
            workshop.isActive(),
            List.of(),
            List.of(),
            workshop.isPinned()
        );
    }
}