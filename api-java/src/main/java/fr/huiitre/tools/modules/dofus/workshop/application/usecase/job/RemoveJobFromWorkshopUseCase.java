package fr.huiitre.tools.modules.dofus.workshop.application.usecase.job;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;

@Service
@Transactional
public class RemoveJobFromWorkshopUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public RemoveJobFromWorkshopUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
    }

    public void execute(Long workshopId, Long jobId) {
        Long userId = authenticatedUserProvider.getUserId();

        workshopRepository.findByIdAndUserId(userId, workshopId)
            .orElseThrow(() -> new WorkshopNotFoundException());

        workshopRepository.removeJobFromWorkshop(userId, workshopId, jobId);
    }
}
