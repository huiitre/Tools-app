package fr.huiitre.tools.modules.dofus.game.api;

import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.dofus.game.application.usecase.GetGameServersByVersionUseCase;
import fr.huiitre.tools.modules.dofus.game.application.usecase.GetGameVersionsUseCase;
import fr.huiitre.tools.modules.dofus.game.application.view.GameServerData;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import io.swagger.v3.oas.annotations.tags.Tag;

@Tag(name = "Dofus")
@RestController
@RequestMapping("/dofus")
public class DofusController {

    private final GetGameServersByVersionUseCase getGameServersByVersionUseCase;

    private static final Logger logger = LoggerFactory.getLogger(DofusController.class);

    private final GetGameVersionsUseCase getGameVersionsUseCase;

    public DofusController(
            GetGameVersionsUseCase getGameVersionsUseCase,
            GetGameServersByVersionUseCase getGameServersByVersionUseCase) {
        this.getGameVersionsUseCase = getGameVersionsUseCase;
        this.getGameServersByVersionUseCase = getGameServersByVersionUseCase;
    }

    @GetMapping("/game-versions")
    public ResponseEntity<List<GameVersionData>> getGameVersions() {
        return ResponseEntity.ok(getGameVersionsUseCase.execute());
    }

    @GetMapping("/game-servers")
    public ResponseEntity<List<GameServerData>> getGameServers(
            @RequestHeader(value = "X-Game-Version-Id", required = true) Long gameVersionId) {
        return ResponseEntity.ok(getGameServersByVersionUseCase.execute(gameVersionId));
    }
}
