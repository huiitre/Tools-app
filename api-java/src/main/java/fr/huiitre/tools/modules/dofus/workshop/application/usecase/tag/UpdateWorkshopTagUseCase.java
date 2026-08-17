package fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.api.dto.UpdateWorkshopTagRequest;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopTagDto;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;

@Service
@Transactional
public class UpdateWorkshopTagUseCase implements SecuredUseCase {

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

    public UpdateWorkshopTagUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopTagRepository workshopTagRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopTagRepository = workshopTagRepository;
    }

    public WorkshopTagDto execute(
            Long tagId,
            UpdateWorkshopTagRequest request) {

        Long userId = authenticatedUserProvider.getUserId();

        WorkshopTag current = workshopTagRepository.findByIdAndUserId(
                userId,
                tagId).orElseThrow(() -> new IllegalArgumentException("Le tag spécifié est introuvable."));

        boolean exists = workshopTagRepository.existsByUserIdAndName(
                userId,
                request.name());

        if (exists && !current.getName().equalsIgnoreCase(request.name())) {
            throw new IllegalArgumentException("Un tag avec ce nom existe déjà.");
        }

        current.update(
                request.name(),
                request.color());

        workshopTagRepository.update(
                userId,
                current);

        return new WorkshopTagDto(
                current.getId(),
                current.getName(),
                current.getColor());
    }
}