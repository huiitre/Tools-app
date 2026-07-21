package fr.huiitre.tools.modules.palworld.infrastructure;

import com.fasterxml.jackson.databind.ObjectMapper;
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
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.*;
import org.springframework.web.client.RestTemplate;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

public class PalworldRestAdapter implements PalworldServerPort {

    private static final Logger log = LoggerFactory.getLogger(PalworldRestAdapter.class);

    private static final String ADMIN_USER = "admin";
    private static final String ADMIN_PASSWORD = "Immortal69";

    private final RestTemplate restTemplate;
    private final ObjectMapper objectMapper;
    private final String baseUrl;

    public PalworldRestAdapter(RestTemplate restTemplate, ObjectMapper objectMapper, String baseUrl) {
        this.restTemplate = restTemplate;
        this.objectMapper = objectMapper;
        this.baseUrl = baseUrl;
    }

    @Override
    public PalworldInfoView getInfo() {
        Map<String, Object> body = get("/v1/api/info", "PALWORLD_INFO_FETCH_FAILED");
        return new PalworldInfoView(
                (String) body.get("version"),
                (String) body.get("servername"),
                (String) body.get("description"),
                (String) body.get("worldguid"));
    }

    @Override
    @SuppressWarnings("unchecked")
    public List<PalworldPlayerView> getPlayers() {
        Map<String, Object> body = get("/v1/api/players", "PALWORLD_PLAYERS_FETCH_FAILED");
        List<Map<String, Object>> players = (List<Map<String, Object>>) body.getOrDefault("players", List.of());

        List<PalworldPlayerView> result = new ArrayList<>();
        for (Map<String, Object> player : players) {
            double locationX = asDouble(player.get("location_x"));
            double locationY = asDouble(player.get("location_y"));
            PalworldCoord.MapPoint mapPoint = PalworldCoord.savToMap(locationX, locationY);

            result.add(new PalworldPlayerView(
                    (String) player.get("name"),
                    (String) player.get("accountName"),
                    (String) player.get("playerId"),
                    (String) player.get("userId"),
                    (String) player.get("iP"),
                    asDouble(player.get("ping")),
                    locationX,
                    locationY,
                    mapPoint.x(),
                    mapPoint.y(),
                    asInt(player.get("level"))));
        }
        return result;
    }

    @Override
    @SuppressWarnings("unchecked")
    public PalworldGameDataView getGameData() {
        Map<String, Object> body = get("/v1/api/game-data", "PALWORLD_GAME_DATA_FETCH_FAILED");
        List<Map<String, Object>> actors = (List<Map<String, Object>>) body.getOrDefault("ActorData", List.of());

        List<Map<String, Object>> playerActors = new ArrayList<>();
        List<Map<String, Object>> otomoPalActors = new ArrayList<>();
        List<Map<String, Object>> baseCampPalActors = new ArrayList<>();
        List<Map<String, Object>> palBoxActors = new ArrayList<>();

        for (Map<String, Object> actor : actors) {
            String type = (String) actor.get("Type");
            String unitType = (String) actor.get("UnitType");
            if ("PalBox".equals(type)) {
                palBoxActors.add(actor);
            } else if ("Character".equals(type) && "Player".equals(unitType)) {
                playerActors.add(actor);
            } else if ("Character".equals(type) && "OtomoPal".equals(unitType)) {
                otomoPalActors.add(actor);
            } else if ("Character".equals(type) && "BaseCampPal".equals(unitType)) {
                baseCampPalActors.add(actor);
            }
        }

        Set<String> onlineGuildIds = new HashSet<>();
        List<PalworldGamePlayerView> players = new ArrayList<>();
        for (Map<String, Object> actor : playerActors) {
            onlineGuildIds.add((String) actor.get("GuildID"));
            players.add(toGamePlayerView(actor, otomoPalActors));
        }

        List<PalworldBaseView> bases = new ArrayList<>();
        for (Map<String, Object> actor : palBoxActors) {
            if (onlineGuildIds.contains(actor.get("GuildID"))) {
                bases.add(toBaseView(actor));
            }
        }

        List<PalworldBasePalView> basePals = new ArrayList<>();
        for (Map<String, Object> actor : baseCampPalActors) {
            if (onlineGuildIds.contains(actor.get("GuildID"))) {
                basePals.add(toBasePalView(actor));
            }
        }

        return new PalworldGameDataView(
                (String) body.get("Time"),
                asDouble(body.get("FPS")),
                asDouble(body.get("AverageFPS")),
                (String) body.get("InGameTime"),
                asInt(body.get("InGameDays")),
                players,
                bases,
                basePals);
    }

    private PalworldGamePlayerView toGamePlayerView(Map<String, Object> actor, List<Map<String, Object>> otomoPalActors) {
        double locationX = asDouble(actor.get("LocationX"));
        double locationY = asDouble(actor.get("LocationY"));
        PalworldCoord.MapPoint mapPoint = PalworldCoord.savToMap(locationX, locationY);
        String instanceId = (String) actor.get("InstanceID");

        PalworldActivePalView activePal = otomoPalActors.stream()
                .filter(pal -> instanceId.equals(pal.get("TrainerInstanceID")))
                .findFirst()
                .map(this::toActivePalView)
                .orElse(null);

        return new PalworldGamePlayerView(
                (String) actor.get("NickName"),
                (String) actor.get("userid"),
                (String) actor.get("ip"),
                asInt(actor.get("level")),
                asInt(actor.get("HP")),
                asInt(actor.get("MaxHP")),
                (String) actor.get("GuildID"),
                (String) actor.get("GuildName"),
                locationX,
                locationY,
                asDouble(actor.get("LocationZ")),
                mapPoint.x(),
                mapPoint.y(),
                asDouble(actor.get("RotationZ")),
                activePal);
    }

    private PalworldActivePalView toActivePalView(Map<String, Object> actor) {
        return new PalworldActivePalView(
                (String) actor.get("NickName"),
                (String) actor.get("Class"),
                asInt(actor.get("level")),
                asInt(actor.get("HP")),
                asInt(actor.get("MaxHP")));
    }

    private PalworldBaseView toBaseView(Map<String, Object> actor) {
        double locationX = asDouble(actor.get("LocationX"));
        double locationY = asDouble(actor.get("LocationY"));
        PalworldCoord.MapPoint mapPoint = PalworldCoord.savToMap(locationX, locationY);
        return new PalworldBaseView(
                (String) actor.get("Name"),
                (String) actor.get("GuildID"),
                (String) actor.get("GuildName"),
                locationX,
                locationY,
                asDouble(actor.get("LocationZ")),
                mapPoint.x(),
                mapPoint.y());
    }

    private PalworldBasePalView toBasePalView(Map<String, Object> actor) {
        double locationX = asDouble(actor.get("LocationX"));
        double locationY = asDouble(actor.get("LocationY"));
        PalworldCoord.MapPoint mapPoint = PalworldCoord.savToMap(locationX, locationY);
        return new PalworldBasePalView(
                (String) actor.get("NickName"),
                (String) actor.get("Class"),
                asInt(actor.get("level")),
                asInt(actor.get("HP")),
                asInt(actor.get("MaxHP")),
                (String) actor.get("GuildID"),
                locationX,
                locationY,
                asDouble(actor.get("LocationZ")),
                mapPoint.x(),
                mapPoint.y());
    }

    @Override
    public PalworldMetricsView getMetrics() {
        Map<String, Object> body = get("/v1/api/metrics", "PALWORLD_METRICS_FETCH_FAILED");
        return new PalworldMetricsView(
                asInt(body.get("currentplayernum")),
                asInt(body.get("serverfps")),
                asDouble(body.get("serverfpsaverage")),
                asDouble(body.get("serverframetime")),
                asInt(body.get("days")),
                asInt(body.get("maxplayernum")),
                asInt(body.get("basecampnum")),
                asLong(body.get("uptime")));
    }

    @Override
    public Map<String, Object> getSettings() {
        return get("/v1/api/settings", "PALWORLD_SETTINGS_FETCH_FAILED");
    }

    @Override
    public void announce(String message) {
        post("/v1/api/announce", Map.of("message", message), "PALWORLD_ANNOUNCE_FAILED");
    }

    @Override
    public void kick(String userId, String message) {
        post("/v1/api/kick", Map.of("userid", userId, "message", message), "PALWORLD_KICK_FAILED");
    }

    @Override
    public void ban(String userId, String message) {
        post("/v1/api/ban", Map.of("userid", userId, "message", message), "PALWORLD_BAN_FAILED");
    }

    @Override
    public void unban(String userId) {
        post("/v1/api/unban", Map.of("userid", userId), "PALWORLD_UNBAN_FAILED");
    }

    @Override
    public void save() {
        post("/v1/api/save", Map.of(), "PALWORLD_SAVE_FAILED");
    }

    @Override
    public void shutdown(int waitTime, String message) {
        post("/v1/api/shutdown", Map.of("waittime", waitTime, "message", message), "PALWORLD_SHUTDOWN_FAILED");
    }

    @Override
    public void stop() {
        post("/v1/api/stop", Map.of(), "PALWORLD_STOP_FAILED");
    }

    private Map<String, Object> get(String path, String errorCode) {
        HttpEntity<Void> request = new HttpEntity<>(authHeaders());
        try {
            ResponseEntity<Map<String, Object>> response = restTemplate.exchange(
                    baseUrl + path, HttpMethod.GET, request,
                    new ParameterizedTypeReference<>() {});

            Map<String, Object> body = response.getBody();
            if (body == null) {
                throw new IllegalArgumentException(errorCode);
            }
            return body;
        } catch (IllegalArgumentException e) {
            throw e;
        } catch (Exception e) {
            log.warn("Appel Palworld GET {} en échec", path, e);
            throw new IllegalArgumentException(errorCode);
        }
    }

    private void post(String path, Map<String, ?> body, String errorCode) {
        byte[] payload;
        try {
            payload = objectMapper.writeValueAsBytes(body);
        } catch (Exception e) {
            throw new IllegalArgumentException(errorCode);
        }

        HttpHeaders headers = authHeaders();
        headers.setContentLength(payload.length);
        HttpEntity<byte[]> request = new HttpEntity<>(payload, headers);

        try {
            restTemplate.exchange(baseUrl + path, HttpMethod.POST, request, Void.class);
        } catch (Exception e) {
            log.warn("Appel Palworld POST {} en échec : {} - {}", path, e.getClass().getSimpleName(), e.getMessage());
            throw new IllegalArgumentException(errorCode);
        }
    }

    private HttpHeaders authHeaders() {
        HttpHeaders headers = new HttpHeaders();
        headers.setBasicAuth(ADMIN_USER, ADMIN_PASSWORD);
        headers.setContentType(MediaType.APPLICATION_JSON);
        return headers;
    }

    private static int asInt(Object value) {
        return value == null ? 0 : ((Number) value).intValue();
    }

    private static long asLong(Object value) {
        return value == null ? 0L : ((Number) value).longValue();
    }

    private static double asDouble(Object value) {
        return value == null ? 0.0 : ((Number) value).doubleValue();
    }
}
