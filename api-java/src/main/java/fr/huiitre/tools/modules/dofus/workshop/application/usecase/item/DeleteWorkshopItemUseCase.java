package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

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
public class DeleteWorkshopItemUseCase implements SecuredUseCase {

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

    public DeleteWorkshopItemUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
    }

    public void execute(Long workshopId, Long workshopItemId) {
        Long userId = authenticatedUserProvider.getUserId();

        boolean exists = workshopRepository.existsByIdAndUserId(userId, workshopId);
        if (!exists) {
            throw new WorkshopNotFoundException();
        }

        workshopRepository.deleteWorkshopItem(userId, workshopId, workshopItemId);
    }
}