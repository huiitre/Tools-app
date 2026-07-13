package fr.huiitre.tools.modules.palworld.application.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.application.command.PalworldBanCommand;
import fr.huiitre.tools.modules.palworld.application.ports.PalworldServerPort;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class BanPalworldPlayerUseCase implements SecuredUseCase {

    private final PalworldServerPort palworldServerPort;

    public BanPalworldPlayerUseCase(PalworldServerPort palworldServerPort) {
        this.palworldServerPort = palworldServerPort;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    public void execute(PalworldBanCommand command) {
        palworldServerPort.ban(command.getUserId(), command.getMessage());
    }
}
