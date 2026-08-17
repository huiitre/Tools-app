package fr.huiitre.tools.modules.dofus.sync.api;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.mail.application.MailReport;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncDofusDataUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus - Sync")
@RestController
@RequestMapping("/dofus/{gameVersionId}/sync")
public class DofusSyncController {

    private final SyncDofusDataUseCase syncDofusDataUseCase;

    private final Logger logger = LoggerFactory.getLogger(DofusSyncController.class);

    public DofusSyncController(SyncDofusDataUseCase syncDofusDataUseCase) {
        this.syncDofusDataUseCase = syncDofusDataUseCase;

    }

    @RequiredRole(RoleCode.TECH)
    @PostMapping
    @ResponseStatus(HttpStatus.OK)
    public MailReport syncDofusData(
            @PathVariable Long gameVersionId) {
        return syncDofusDataUseCase.execute(gameVersionId);
    }
}
