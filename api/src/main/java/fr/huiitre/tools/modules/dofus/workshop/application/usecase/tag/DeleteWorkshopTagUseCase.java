package fr.huiitre.tools.modules.dofus.workshop.application.usecase.tag;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;

@Service
@Transactional
public class DeleteWorkshopTagUseCase implements SecuredUseCase {

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

    public DeleteWorkshopTagUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopTagRepository workshopTagRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopTagRepository = workshopTagRepository;
    }

    public void execute(Long tagId) {
        Long userId = authenticatedUserProvider.getUserId();

        boolean exists = workshopTagRepository.existsByIdAndUserId(
                userId,
                tagId);

        if (!exists) {
            throw new IllegalArgumentException("Le tag spécifié est introuvable.");
        }

        workshopTagRepository.delete(
                userId,
                tagId);
    }
}