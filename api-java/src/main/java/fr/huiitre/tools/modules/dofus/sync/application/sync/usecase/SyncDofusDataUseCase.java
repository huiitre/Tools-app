package fr.huiitre.tools.modules.dofus.sync.application.sync.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.mail.application.MailReport;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;

@Service
@Transactional
public class SyncDofusDataUseCase implements SecuredUseCase {

    private final GameVersionRepository gameVersionRepository;

    private final SyncDofus3DataUseCase syncDofus3DataUseCase;
    private final SyncRetroDataUseCase syncRetroDataUseCase;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public SyncDofusDataUseCase(
            SyncDofus3DataUseCase syncDofus3DataUseCase,
            SyncRetroDataUseCase syncRetroDataUseCase,
            GameVersionRepository gameVersionRepository) {
        this.syncDofus3DataUseCase = syncDofus3DataUseCase;
        this.gameVersionRepository = gameVersionRepository;
        this.syncRetroDataUseCase = syncRetroDataUseCase;
    }

    public MailReport execute(
            Long gameVersionId) {
        // * récupération de la version */
        GameVersionData gameVersion = gameVersionRepository.findById(gameVersionId)
                .orElseThrow(() -> new IllegalArgumentException("Game version not found: " + gameVersionId));

        switch (gameVersion.getCode()) {
            case "dofus3" -> {
                return syncDofus3DataUseCase.execute(gameVersion);
            }
            case "retro" -> {
                // throw new IllegalArgumentException("Unsupported game version: retro");
                return syncRetroDataUseCase.execute(gameVersion);
            }
            default -> throw new IllegalArgumentException("Unsupported game version: " + gameVersion.getCode());
        }
    }
}