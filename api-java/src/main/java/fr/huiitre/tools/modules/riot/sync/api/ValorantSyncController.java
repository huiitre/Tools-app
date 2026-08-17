package fr.huiitre.tools.modules.riot.sync.api;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.riot.sync.application.ValorantGlobalSyncReport;
import fr.huiitre.tools.modules.riot.sync.application.usecase.SyncValorantUseCase;

@RestController
@RequestMapping("/riot/valorant/sync")
public class ValorantSyncController {

    private final SyncValorantUseCase syncValorantUseCase;

    public ValorantSyncController(SyncValorantUseCase syncValorantUseCase) {
        this.syncValorantUseCase = syncValorantUseCase;
    }

    @RequiredRole(RoleCode.TECH)
    @PostMapping
    @ResponseStatus(HttpStatus.OK)
    public ValorantGlobalSyncReport sync() {
        return syncValorantUseCase.execute();
    }
}
