package fr.huiitre.tools.modules.dofus.game.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameServerRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameServerData;

@Service
@Transactional
public class GetGameServersByVersionUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final GameServerRepository gameServerRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetGameServersByVersionUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            GameServerRepository gameServerRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.gameServerRepository = gameServerRepository;
    }

    public List<GameServerData> execute(Long gameVersionId) {
        List<GameServerData> gameServers = gameServerRepository.findAllByGameVersionId(gameVersionId);
        return gameServers;
    }
}