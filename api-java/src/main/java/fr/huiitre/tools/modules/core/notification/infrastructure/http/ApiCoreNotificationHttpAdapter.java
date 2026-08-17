package fr.huiitre.tools.modules.core.notification.infrastructure.http;

import java.util.HashMap;
import java.util.Map;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.client.RestTemplate;

import fr.huiitre.tools.modules.core.notification.application.port.ApiCoreNotificationPort;
import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

public class ApiCoreNotificationHttpAdapter implements ApiCoreNotificationPort {

    private static final Logger log = LoggerFactory.getLogger(ApiCoreNotificationHttpAdapter.class);
    private static final String INTERNAL_TOKEN_HEADER = "X-Internal-Token";

    private final RestTemplate restTemplate;
    private final String baseUrl;
    private final String internalToken;

    public ApiCoreNotificationHttpAdapter(RestTemplate restTemplate, String baseUrl, String internalToken) {
        this.restTemplate = restTemplate;
        this.baseUrl = baseUrl;
        this.internalToken = internalToken;
    }

    // Fail-open : une notification manquée ne doit jamais faire échouer le flux métier appelant.
    @Override
    public Optional<Long> publish(
            String title,
            String body,
            NotificationType type,
            Long targetUserId,
            RoleCode targetMinRole,
            Long targetModuleId,
            String metadata) {

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        headers.set(INTERNAL_TOKEN_HEADER, internalToken);

        Map<String, Object> payload = new HashMap<>();
        payload.put("title", title);
        payload.put("body", body);
        payload.put("type", type.name());
        payload.put("targetUserId", targetUserId);
        payload.put("targetMinRole", targetMinRole != null ? targetMinRole.name() : null);
        payload.put("targetModuleId", targetModuleId);
        payload.put("metadata", metadata);

        try {
            ResponseEntity<Map<String, Object>> response = restTemplate.exchange(
                    baseUrl + "/internal/notifications",
                    HttpMethod.POST,
                    new HttpEntity<>(payload, headers),
                    new ParameterizedTypeReference<>() {});

            Map<String, Object> responseBody = response.getBody();
            if (response.getStatusCode() == HttpStatus.NO_CONTENT || responseBody == null) {
                return Optional.empty();
            }
            return Optional.ofNullable(responseBody.get("id"))
                    .map(id -> ((Number) id).longValue());

        } catch (Exception e) {
            log.warn(
                    "Publication de notification vers l'API Core en échec : {} - {}",
                    e.getClass().getSimpleName(),
                    e.getMessage());
            return Optional.empty();
        }
    }
}
