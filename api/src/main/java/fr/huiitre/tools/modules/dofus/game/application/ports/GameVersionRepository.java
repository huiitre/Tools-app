package fr.huiitre.tools.modules.dofus.game.application.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;

public interface GameVersionRepository {

    Optional<GameVersionData> findById(Long gameVersionId);

    GameVersionData findByCode(String code);

    List<GameVersionData> findAll();

    Optional<GameVersionData> findByGameServerId(Long gameServerId); 
}
