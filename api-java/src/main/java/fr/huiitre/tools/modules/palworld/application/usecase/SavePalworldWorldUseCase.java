package fr.huiitre.tools.modules.palworld.application.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.application.ports.PalworldServerPort;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class SavePalworldWorldUseCase implements SecuredUseCase {

    private final PalworldServerPort palworldServerPort;

    public SavePalworldWorldUseCase(PalworldServerPort palworldServerPort) {
        this.palworldServerPort = palworldServerPort;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.MODERATOR;
    }

    public void execute() {
        palworldServerPort.save();
    }
}
