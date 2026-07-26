package fr.huiitre.tools.modules.palworld.sync.api;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldGlobalSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.usecase.SyncPalworldUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Palworld - Sync")
@RestController
@RequestMapping("/palworld/sync")
public class PalworldSyncController {

    private final SyncPalworldUseCase syncPalworldUseCase;

    public PalworldSyncController(SyncPalworldUseCase syncPalworldUseCase) {
        this.syncPalworldUseCase = syncPalworldUseCase;
    }

    @RequiredRole(RoleCode.TECH)
    @PostMapping
    @ResponseStatus(HttpStatus.OK)
    public PalworldGlobalSyncReport sync() {
        return syncPalworldUseCase.execute();
    }
}
