package fr.huiitre.tools.modules.palworld.application.ports;

import fr.huiitre.tools.modules.palworld.application.view.PalworldGameDataView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldInfoView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldMetricsView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldPlayerView;

import java.util.List;
import java.util.Map;

public interface PalworldServerPort {

    PalworldInfoView getInfo();

    List<PalworldPlayerView> getPlayers();

    PalworldGameDataView getGameData();

    PalworldMetricsView getMetrics();

    Map<String, Object> getSettings();

    void announce(String message);

    void kick(String userId, String message);

    void ban(String userId, String message);

    void unban(String userId);

    void save();

    void shutdown(int waitTime, String message);

    void stop();
}
