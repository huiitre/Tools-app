package fr.huiitre.tools.modules.dofus.workshop.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopLinkNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;

@Service
@Transactional
public class DeleteWorkshopLinkUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;

    public DeleteWorkshopLinkUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopRepository workshopRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public void execute(Long workshopId, Long linkId) {
        Long userId = authenticatedUserProvider.getUserId();

        if (!workshopRepository.existsByIdAndUserId(userId, workshopId)) {
            throw new WorkshopNotFoundException();
        }

        int deleted = workshopRepository.deleteLink(userId, workshopId, linkId);
        if (deleted == 0) {
            throw new WorkshopLinkNotFoundException();
        }
    }
}
