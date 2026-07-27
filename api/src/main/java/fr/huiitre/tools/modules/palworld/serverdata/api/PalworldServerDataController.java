package fr.huiitre.tools.modules.palworld.serverdata.api;

import java.util.Map;
import java.util.UUID;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.GetBasePalsUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.GetGuildsUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.GetPalInstanceHistoryUseCase;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.SyncServerDataUseCase;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Palworld - Server data")
@RestController
@RequestMapping("/palworld/server-data")
public class PalworldServerDataController {

    private static final String DISABLED_MESSAGE = "Endpoint désactivé.";

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
    public ResponseEntity<Map<String, String>> sync() {
        return disabled();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/guilds")
    public ResponseEntity<Map<String, String>> getGuilds() {
        return disabled();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/bases/{baseId}/pals")
    public ResponseEntity<Map<String, String>> getBasePals(@PathVariable UUID baseId) {
        return disabled();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/pal-instances/{instanceId}/history")
    public ResponseEntity<Map<String, String>> getPalInstanceHistory(@PathVariable UUID instanceId) {
        return disabled();
    }

    private ResponseEntity<Map<String, String>> disabled() {
        return ResponseEntity.status(HttpStatus.GONE).body(Map.of("message", DISABLED_MESSAGE));
    }
}
