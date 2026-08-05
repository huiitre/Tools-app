package fr.huiitre.tools.modules.palworld.serverdata.api;

import java.util.List;
import java.util.UUID;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.serverdata.application.ServerDataSyncReport;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.GetBasePalsUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.GetGuildsUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.GetPalInstanceHistoryUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.SyncServerDataUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.GuildSummaryView;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSnapshotView;
import fr.huiitre.tools.modules.palworld.serverdata.application.view.PalInstanceSummaryView;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Palworld - Server data")
@RestController
@RequestMapping("/palworld/server-data")
public class PalworldServerDataController {

    private final SyncServerDataUseCase syncServerDataUseCase;
    private final GetGuildsUseCase getGuildsUseCase;
    private final GetBasePalsUseCase getBasePalsUseCase;
    private final GetPalInstanceHistoryUseCase getPalInstanceHistoryUseCase;

    public PalworldServerDataController(
            SyncServerDataUseCase syncServerDataUseCase,
            GetGuildsUseCase getGuildsUseCase,
            GetBasePalsUseCase getBasePalsUseCase,
            GetPalInstanceHistoryUseCase getPalInstanceHistoryUseCase) {
        this.syncServerDataUseCase = syncServerDataUseCase;
        this.getGuildsUseCase = getGuildsUseCase;
        this.getBasePalsUseCase = getBasePalsUseCase;
        this.getPalInstanceHistoryUseCase = getPalInstanceHistoryUseCase;
    }

    @RequiredRole(RoleCode.TECH)
    @PostMapping("/admin/sync")
    @ResponseStatus(HttpStatus.OK)
    public ServerDataSyncReport sync() {
        return syncServerDataUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/guilds")
    @ResponseStatus(HttpStatus.OK)
    public List<GuildSummaryView> getGuilds() {
        return getGuildsUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/bases/{baseId}/pals")
    @ResponseStatus(HttpStatus.OK)
    public List<PalInstanceSummaryView> getBasePals(@PathVariable UUID baseId) {
        return getBasePalsUseCase.execute(baseId);
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/pal-instances/{instanceId}/history")
    @ResponseStatus(HttpStatus.OK)
    public List<PalInstanceSnapshotView> getPalInstanceHistory(@PathVariable UUID instanceId) {
        return getPalInstanceHistoryUseCase.execute(instanceId);
    }
}
