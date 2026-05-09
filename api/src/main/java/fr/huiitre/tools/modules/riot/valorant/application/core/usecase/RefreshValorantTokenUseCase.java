package fr.huiitre.tools.modules.riot.valorant.application.core.usecase;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.riot.valorant.application.core.command.RefreshTokenCommand;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.core.view.ValorantTokenView;

import java.util.Optional;

import org.springframework.stereotype.Service;

@Service
public class RefreshValorantTokenUseCase implements SecuredUseCase {

    private final RiotAuthPort riotAuthPort;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.RIOT);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public RefreshValorantTokenUseCase(RiotAuthPort riotAuthPort) {
        this.riotAuthPort = riotAuthPort;
    }

    public ValorantTokenView execute(RefreshTokenCommand command) {
        if (command.getRefreshToken() == null || command.getRefreshToken().isBlank()) {
            throw new IllegalArgumentException("REFRESH_TOKEN_REQUIRED");
        }
        return riotAuthPort.refresh(command.getRefreshToken());
    }
}
