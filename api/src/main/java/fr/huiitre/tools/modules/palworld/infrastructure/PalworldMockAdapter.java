package fr.huiitre.tools.modules.palworld.infrastructure;

import fr.huiitre.tools.modules.palworld.application.ports.PalworldServerPort;
import fr.huiitre.tools.modules.palworld.application.view.PalworldActivePalView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldBasePalView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldBaseView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldGameDataView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldGamePlayerView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldInfoView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldMetricsView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldPlayerView;
import fr.huiitre.tools.modules.palworld.domain.PalworldCoord;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.List;
import java.util.Map;

public class PalworldMockAdapter implements PalworldServerPort {

    private static final Logger log = LoggerFactory.getLogger(PalworldMockAdapter.class);

    @Override
    public PalworldInfoView getInfo() {
        return new PalworldInfoView("v1.13.0.0", "Serveur de Test (mock)", "Données simulées — pas de serveur réel", "MOCK-WORLD-GUID");
    }

    @Override
    public List<PalworldPlayerView> getPlayers() {
        return List.of(
                mockPlayer("Huiitre", "A6F5A08F000000000000000000000001", "steam_76561198000000001", -151431.796875, -61798.30078125, 42),
                mockPlayer("Copain1", "A6F5A08F000000000000000000000002", "steam_76561198000000002", -332013, -234790.5, 17),
                mockPlayer("Copain2", "A6F5A08F000000000000000000000003", "steam_76561198000000003", -265342.59375, -48874.3984375, 55));
    }

    private PalworldPlayerView mockPlayer(String name, String playerId, String userId, double locationX, double locationY, int level) {
        PalworldCoord.MapPoint mapPoint = PalworldCoord.savToMap(locationX, locationY);
        return new PalworldPlayerView(name, name, playerId, userId, "127.0.0.1", 42.0, locationX, locationY, mapPoint.x(), mapPoint.y(), level);
    }

    @Override
    public PalworldGameDataView getGameData() {
        PalworldCoord.MapPoint huiitreMap = PalworldCoord.savToMap(-151431.796875, -61798.30078125);
        PalworldCoord.MapPoint baseMap = PalworldCoord.savToMap(-151252.09375, -61435.3984375);

        PalworldGamePlayerView huiitre = new PalworldGamePlayerView(
                "Huiitre", "steam_76561198000000001", "127.0.0.1", 42, 3200, 3200,
                "MOCK-GUILD-0001", "FreshWomen Crew (mock)",
                -151431.796875, -61798.30078125, -1317.8,
                huiitreMap.x(), huiitreMap.y(), -68.0,
                new PalworldActivePalView("Fenglope", "BP_FengyunDeeper_BOSS_C", 53, 5359, 5359));

        PalworldBaseView base = new PalworldBaseView(
                "Base principale (mock)", "MOCK-GUILD-0001", "FreshWomen Crew (mock)",
                -151252.09375, -61435.3984375, -890.0, baseMap.x(), baseMap.y());

        PalworldBasePalView basePal = new PalworldBasePalView(
                "Incineram", "BP_Baphomet_C", 27, 2080, 2080, "MOCK-GUILD-0001",
                -151302.09375, -61651.69921875, -1270.0, baseMap.x(), baseMap.y());

        return new PalworldGameDataView(
                "2026-07-21 16:09:06", 59.6, 59.3, "06:22", 319,
                List.of(huiitre), List.of(base), List.of(basePal));
    }

    @Override
    public PalworldMetricsView getMetrics() {
        return new PalworldMetricsView(3, 60, 59.8, 16.7, 317, 32, 10, 3600L * 12);
    }

    @Override
    public Map<String, Object> getSettings() {
        return Map.of(
                "Difficulty", "None",
                "DayTimeSpeedRate", 1.0,
                "ExpRate", 1.0,
                "PalCaptureRate", 2.0);
    }

    @Override
    public void announce(String message) {
        log.info("[MOCK] announce: {}", message);
    }

    @Override
    public void kick(String userId, String message) {
        log.info("[MOCK] kick userId={} message={}", userId, message);
    }

    @Override
    public void ban(String userId, String message) {
        log.info("[MOCK] ban userId={} message={}", userId, message);
    }

    @Override
    public void unban(String userId) {
        log.info("[MOCK] unban userId={}", userId);
    }

    @Override
    public void save() {
        log.info("[MOCK] save");
    }

    @Override
    public void shutdown(int waitTime, String message) {
        log.info("[MOCK] shutdown waitTime={} message={}", waitTime, message);
    }

    @Override
    public void stop() {
        log.info("[MOCK] stop");
    }
}
