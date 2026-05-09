package fr.huiitre.tools.modules.riot.sync.infrastructure;

import org.springframework.core.ParameterizedTypeReference;
import org.springframework.http.HttpMethod;
import org.springframework.web.client.RestTemplate;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinSyncData;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;

public class ValorantApiSkinDataProvider implements ValorantSkinDataProvider {

    private static final String SKINS_URL = "https://valorant-api.com/v1/weapons/skins?language=fr-FR";

    private final RestTemplate restTemplate;

    public ValorantApiSkinDataProvider(RestTemplate restTemplate) {
        this.restTemplate = restTemplate;
    }

    @Override
    @SuppressWarnings("unchecked")
    public List<ValorantSkinSyncData> fetchAll() {
        Map<String, Object> body = restTemplate.exchange(
                SKINS_URL, HttpMethod.GET, null,
                new ParameterizedTypeReference<Map<String, Object>>() {}).getBody();

        if (body == null) throw new IllegalStateException("Empty response from Valorant API");

        List<Map<String, Object>> data = (List<Map<String, Object>>) body.get("data");
        List<ValorantSkinSyncData> result = new ArrayList<>();

        for (Map<String, Object> skin : data) {
            UUID assetId = UUID.fromString((String) skin.get("uuid"));
            String name = (String) skin.get("displayName");
            String iconUrl = resolveIconUrl(skin);
            UUID tierUuid = parseUuid((String) skin.get("themeUuid"));
            UUID contentTierUuid = parseUuid((String) skin.get("contentTierUuid"));

            result.add(new ValorantSkinSyncData(assetId, name, iconUrl, tierUuid, contentTierUuid, null, List.of()));
        }

        return result;
    }

    @SuppressWarnings("unchecked")
    private String resolveIconUrl(Map<String, Object> skin) {
        String icon = (String) skin.get("displayIcon");
        if (icon != null) return icon;

        List<Map<String, Object>> levels = (List<Map<String, Object>>) skin.get("levels");
        if (levels != null && !levels.isEmpty()) {
            return (String) levels.get(0).get("displayIcon");
        }
        return null;
    }

    private UUID parseUuid(String value) {
        return value != null ? UUID.fromString(value) : null;
    }
}
