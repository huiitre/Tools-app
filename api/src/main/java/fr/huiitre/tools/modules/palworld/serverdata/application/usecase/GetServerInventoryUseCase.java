package fr.huiitre.tools.modules.palworld.serverdata.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.ports.ServerInventoryQueryRepository;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.ServerDataInventoryView;

@Service
public class GetServerInventoryUseCase implements SecuredUseCase {

    private final ServerInventoryQueryRepository serverInventoryQueryRepository;

    public GetServerInventoryUseCase(ServerInventoryQueryRepository serverInventoryQueryRepository) {
        this.serverInventoryQueryRepository = serverInventoryQueryRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public ServerDataInventoryView execute() {
        return serverInventoryQueryRepository.getCurrentInventory();
    }
}
