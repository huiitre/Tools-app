package fr.huiitre.tools.modules.dofus.game.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;

@Service
@Transactional
public class GetGameVersionsUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final GameVersionRepository gameVersionRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetGameVersionsUseCase(
            GameVersionRepository gameVersionRepository,
            AuthenticatedUserProvider authenticatedUserProvider) {
        this.gameVersionRepository = gameVersionRepository;
        this.authenticatedUserProvider = authenticatedUserProvider;
    }

    public List<GameVersionData> execute() {

        return gameVersionRepository.findAll();
    }
}