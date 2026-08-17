package fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.CreateWorkshopTagRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

@Service
@Transactional
public class CreateWorkshopTagUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final WorkshopTagRepository workshopTagRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public CreateWorkshopTagUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopTagRepository workshopTagRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopTagRepository = workshopTagRepository;
    }

    public WorkshopTagDto execute(
            Long gameVersionId,
            CreateWorkshopTagRequest request) {

        Long userId = authenticatedUserProvider.getUserId();

        boolean exists = workshopTagRepository.existsByUserIdAndName(
                userId,
                request.name());

        if (exists) {
            throw new IllegalArgumentException("Un tag avec ce nom existe déjà.");
        }

        WorkshopTag tag = WorkshopTag.create(request.name(), request.color());

        Long id = workshopTagRepository.create(
                gameVersionId,
                userId,
                tag);

        return new WorkshopTagDto(
                id,
                tag.getName(),
                tag.getColor());
    }
}