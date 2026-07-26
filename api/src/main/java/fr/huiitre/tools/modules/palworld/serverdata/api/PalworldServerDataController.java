package fr.huiitre.tools.modules.palworld.serverdata.api;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.serverdata.application.ServerDataSyncReport;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.SyncServerDataUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Palworld - Server data")
@RestController
@RequestMapping("/palworld/server-data")
public class PalworldServerDataController {

    private final SyncServerDataUseCase syncServerDataUseCase;

    public PalworldServerDataController(SyncServerDataUseCase syncServerDataUseCase) {
        this.syncServerDataUseCase = syncServerDataUseCase;
    }

    @RequiredRole(RoleCode.TECH)
    @PostMapping("/admin/sync")
    @ResponseStatus(HttpStatus.OK)
    public ServerDataSyncReport sync() {
        return syncServerDataUseCase.execute();
    }
}
