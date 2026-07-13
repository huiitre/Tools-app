package fr.huiitre.tools.modules.palworld.api;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.application.command.PalworldAnnounceCommand;
import fr.huiitre.tools.modules.palworld.application.command.PalworldBanCommand;
import fr.huiitre.tools.modules.palworld.application.command.PalworldKickCommand;
import fr.huiitre.tools.modules.palworld.application.command.PalworldShutdownCommand;
import fr.huiitre.tools.modules.palworld.application.command.PalworldUnbanCommand;
import fr.huiitre.tools.modules.palworld.application.usecase.AnnouncePalworldMessageUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.BanPalworldPlayerUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.GetPalworldInfoUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.GetPalworldMetricsUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.GetPalworldPlayersUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.GetPalworldSettingsUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.KickPalworldPlayerUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.SavePalworldWorldUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.ShutdownPalworldServerUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.StopPalworldServerUseCase;
import fr.huiitre.tools.modules.palworld.application.usecase.UnbanPalworldPlayerUseCase;
import fr.huiitre.tools.modules.palworld.application.view.PalworldInfoView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldMetricsView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldPlayerView;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/palworld/server")
public class PalworldServerController {

    private final GetPalworldInfoUseCase getPalworldInfoUseCase;
    private final GetPalworldPlayersUseCase getPalworldPlayersUseCase;
    private final GetPalworldMetricsUseCase getPalworldMetricsUseCase;
    private final GetPalworldSettingsUseCase getPalworldSettingsUseCase;
    private final AnnouncePalworldMessageUseCase announcePalworldMessageUseCase;
    private final KickPalworldPlayerUseCase kickPalworldPlayerUseCase;
    private final SavePalworldWorldUseCase savePalworldWorldUseCase;
    private final BanPalworldPlayerUseCase banPalworldPlayerUseCase;
    private final UnbanPalworldPlayerUseCase unbanPalworldPlayerUseCase;
    private final ShutdownPalworldServerUseCase shutdownPalworldServerUseCase;
    private final StopPalworldServerUseCase stopPalworldServerUseCase;

    public PalworldServerController(
            GetPalworldInfoUseCase getPalworldInfoUseCase,
            GetPalworldPlayersUseCase getPalworldPlayersUseCase,
            GetPalworldMetricsUseCase getPalworldMetricsUseCase,
            GetPalworldSettingsUseCase getPalworldSettingsUseCase,
            AnnouncePalworldMessageUseCase announcePalworldMessageUseCase,
            KickPalworldPlayerUseCase kickPalworldPlayerUseCase,
            SavePalworldWorldUseCase savePalworldWorldUseCase,
            BanPalworldPlayerUseCase banPalworldPlayerUseCase,
            UnbanPalworldPlayerUseCase unbanPalworldPlayerUseCase,
            ShutdownPalworldServerUseCase shutdownPalworldServerUseCase,
            StopPalworldServerUseCase stopPalworldServerUseCase) {
        this.getPalworldInfoUseCase = getPalworldInfoUseCase;
        this.getPalworldPlayersUseCase = getPalworldPlayersUseCase;
        this.getPalworldMetricsUseCase = getPalworldMetricsUseCase;
        this.getPalworldSettingsUseCase = getPalworldSettingsUseCase;
        this.announcePalworldMessageUseCase = announcePalworldMessageUseCase;
        this.kickPalworldPlayerUseCase = kickPalworldPlayerUseCase;
        this.savePalworldWorldUseCase = savePalworldWorldUseCase;
        this.banPalworldPlayerUseCase = banPalworldPlayerUseCase;
        this.unbanPalworldPlayerUseCase = unbanPalworldPlayerUseCase;
        this.shutdownPalworldServerUseCase = shutdownPalworldServerUseCase;
        this.stopPalworldServerUseCase = stopPalworldServerUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/info")
    public PalworldInfoView getInfo() {
        return getPalworldInfoUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/players")
    public List<PalworldPlayerView> getPlayers() {
        return getPalworldPlayersUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/metrics")
    public PalworldMetricsView getMetrics() {
        return getPalworldMetricsUseCase.execute();
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping("/settings")
    public Map<String, Object> getSettings() {
        return getPalworldSettingsUseCase.execute();
    }

    @RequiredRole(RoleCode.MODERATOR)
    @PostMapping("/announce")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void announce(@RequestBody PalworldAnnounceCommand command) {
        announcePalworldMessageUseCase.execute(command);
    }

    @RequiredRole(RoleCode.MODERATOR)
    @PostMapping("/kick")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void kick(@RequestBody PalworldKickCommand command) {
        kickPalworldPlayerUseCase.execute(command);
    }

    @RequiredRole(RoleCode.MODERATOR)
    @PostMapping("/save")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void save() {
        savePalworldWorldUseCase.execute();
    }

    @RequiredRole(RoleCode.ADMIN)
    @PostMapping("/ban")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void ban(@RequestBody PalworldBanCommand command) {
        banPalworldPlayerUseCase.execute(command);
    }

    @RequiredRole(RoleCode.ADMIN)
    @PostMapping("/unban")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void unban(@RequestBody PalworldUnbanCommand command) {
        unbanPalworldPlayerUseCase.execute(command);
    }

    @RequiredRole(RoleCode.ADMIN)
    @PostMapping("/shutdown")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void shutdown(@RequestBody PalworldShutdownCommand command) {
        shutdownPalworldServerUseCase.execute(command);
    }

    @RequiredRole(RoleCode.ADMIN)
    @PostMapping("/stop")
    @ResponseStatus(HttpStatus.NO_CONTENT)
    public void stop() {
        stopPalworldServerUseCase.execute();
    }
}
