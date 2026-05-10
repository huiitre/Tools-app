package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.*;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestTemplate;

import java.time.LocalDateTime;
import java.util.Base64;
import java.util.Map;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.core.type.TypeReference;

public class RiotAuthHttpAdapter implements RiotAuthPort {

    private static final String TOKEN_URL = "https://auth.riotgames.com/token";
    private static final String CLIENT_ID = "prod-xsso-playvalorant";

    private final RestTemplate restTemplate;
    private final ObjectMapper objectMapper;

    public RiotAuthHttpAdapter(RestTemplate restTemplate, ObjectMapper objectMapper) {
        this.restTemplate = restTemplate;
        this.objectMapper = objectMapper;
    }

    @Override
    public ValorantAuthResponse refresh(String refreshToken) {
        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);

        MultiValueMap<String, String> body = new LinkedMultiValueMap<>();
        body.add("grant_type", "refresh_token");
        body.add("refresh_token", refreshToken);
        body.add("client_id", CLIENT_ID);

        HttpEntity<MultiValueMap<String, String>> request = new HttpEntity<>(body, headers);

        try {
            ResponseEntity<Map<String, Object>> response = restTemplate.exchange(
                    TOKEN_URL, HttpMethod.POST, request,
                    new ParameterizedTypeReference<>() {});

            Map<String, Object> responseBody = response.getBody();
            if (responseBody == null) {
                throw new IllegalArgumentException("RIOT_AUTH_EMPTY_RESPONSE");
            }

            String accessToken = (String) responseBody.get("access_token");
            String newRefreshToken = (String) responseBody.get("refresh_token");
            
            // Extraction du PUUID depuis le JWT Access Token
            String puuid = extractPuuid(accessToken);
            
            // Par défaut, le refresh token Riot dure 30 jours
            LocalDateTime expiresAt = LocalDateTime.now().plusDays(30);

            return new ValorantAuthResponse(accessToken, newRefreshToken, puuid, expiresAt);

        } catch (HttpClientErrorException e) {
            throw new IllegalArgumentException("RIOT_TOKEN_INVALID");
        } catch (Exception e) {
            throw new RuntimeException("Erreur technique lors du refresh Riot", e);
        }
    }

    private String extractPuuid(String accessToken) {
        try {
            String[] parts = accessToken.split("\\.");
            if (parts.length < 2) throw new IllegalArgumentException("INVALID_JWT");
            
            byte[] payloadBytes = Base64.getUrlDecoder().decode(parts[1]);
            Map<String, Object> payload = objectMapper.readValue(payloadBytes, new TypeReference<Map<String, Object>>() {});
            return (String) payload.get("sub");
        } catch (Exception e) {
            throw new RuntimeException("Impossible d'extraire le PUUID du token", e);
        }
    }
}
