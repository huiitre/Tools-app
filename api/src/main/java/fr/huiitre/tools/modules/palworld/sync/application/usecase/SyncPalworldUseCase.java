package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldGlobalSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncResult;

@Service
@Transactional
public class SyncPalworldUseCase implements SecuredUseCase {

    private final SyncElementsUseCase syncElementsUseCase;
    private final SyncWorkSuitabilitiesUseCase syncWorkSuitabilitiesUseCase;
    private final SyncSkillsUseCase syncSkillsUseCase;
    private final SyncPalsUseCase syncPalsUseCase;

    public SyncPalworldUseCase(
            SyncElementsUseCase syncElementsUseCase,
            SyncWorkSuitabilitiesUseCase syncWorkSuitabilitiesUseCase,
            SyncSkillsUseCase syncSkillsUseCase,
            SyncPalsUseCase syncPalsUseCase) {
        this.syncElementsUseCase = syncElementsUseCase;
        this.syncWorkSuitabilitiesUseCase = syncWorkSuitabilitiesUseCase;
        this.syncSkillsUseCase = syncSkillsUseCase;
        this.syncPalsUseCase = syncPalsUseCase;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public PalworldGlobalSyncReport execute() {
        ElementSyncResult elements = syncElementsUseCase.execute();
        WorkSuitabilitySyncResult workSuitabilities = syncWorkSuitabilitiesUseCase.execute();
        SkillSyncResult skills = syncSkillsUseCase.execute(elements.idByExternalCode());
        PalworldSyncReport pals = syncPalsUseCase.execute(
                elements.idByExternalCode(),
                workSuitabilities.idBySlug(),
                skills.idBySlug());

        return new PalworldGlobalSyncReport(elements.report(), workSuitabilities.report(), skills.report(), pals);
    }
}
