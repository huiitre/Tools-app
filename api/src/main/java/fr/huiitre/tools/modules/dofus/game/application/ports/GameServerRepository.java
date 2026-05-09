package fr.huiitre.tools.modules.dofus.game.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.game.application.view.GameServerData;

public interface GameServerRepository {

    List<GameServerData> findAllByGameVersionId(Long gameVersionId);
}
