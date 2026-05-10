package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.stereotype.Component;

import java.util.Base64;
import java.util.Map;

@Component
public class ValorantTokenParser {

    private final ObjectMapper objectMapper;

    public ValorantTokenParser(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
    }

    public String extractPuuid(String accessToken) {
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
