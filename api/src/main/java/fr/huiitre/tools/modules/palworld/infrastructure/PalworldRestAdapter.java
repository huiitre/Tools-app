package fr.huiitre.tools.modules.palworld.infrastructure;

import fr.huiitre.tools.modules.palworld.application.ports.PalworldServerPort;
import fr.huiitre.tools.modules.palworld.application.view.PalworldInfoView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldMetricsView;
import fr.huiitre.tools.modules.palworld.application.view.PalworldPlayerView;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.*;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestTemplate;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class PalworldRestAdapter implements PalworldServerPort {

    private static final String ADMIN_USER = "admin";
    private static final String ADMIN_PASSWORD = "Immortal69";

    private final RestTemplate restTemplate;
    private final String baseUrl;

    public PalworldRestAdapter(RestTemplate restTemplate, String baseUrl) {
        this.restTemplate = restTemplate;
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
            result.add(new PalworldPlayerView(
                    (String) player.get("name"),
                    (String) player.get("accountName"),
                    (String) player.get("playerId"),
                    (String) player.get("userId"),
                    (String) player.get("ip"),
                    asDouble(player.get("ping")),
                    asDouble(player.get("location_x")),
                    asDouble(player.get("location_y")),
                    asInt(player.get("level")),
                    asInt(player.get("building_count"))));
        }
        return result;
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
        } catch (HttpClientErrorException e) {
            throw new IllegalArgumentException(errorCode);
        }
    }

    private void post(String path, Map<String, ?> body, String errorCode) {
        HttpEntity<Map<String, ?>> request = new HttpEntity<>(body, authHeaders());
        try {
            restTemplate.exchange(baseUrl + path, HttpMethod.POST, request, Void.class);
        } catch (HttpClientErrorException e) {
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
