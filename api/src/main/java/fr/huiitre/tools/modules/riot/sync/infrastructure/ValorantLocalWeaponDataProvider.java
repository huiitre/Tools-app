package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponSyncData;

public class ValorantLocalWeaponDataProvider implements ValorantWeaponDataProvider {

    private static final String CATEGORY_PREFIX = "EEquippableCategory::";

    private final ValorantLocalAssetsReader assetsReader;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public ValorantLocalWeaponDataProvider(ValorantLocalAssetsReader assetsReader, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<ValorantWeaponSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("weapons.json");
            JsonNode root = objectMapper.readTree(json);
            JsonNode data = root.path("data");

            List<ValorantWeaponSyncData> result = new ArrayList<>();

            for (JsonNode weapon : data) {
                UUID assetId = UUID.fromString(weapon.get("uuid").asText());
                String name = weapon.path("displayName").asText(null);
                String rawCategory = weapon.path("category").asText(null);
                String category = rawCategory != null ? rawCategory.replace(CATEGORY_PREFIX, "") : null;
                UUID defaultSkinAssetId = parseUuid(weapon.path("defaultSkinUuid").asText(null));
                String displayIconUrl = resolveIconUrl(assetId, weapon);

                result.add(new ValorantWeaponSyncData(assetId, name, category, defaultSkinAssetId, displayIconUrl));
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Valorant weapons from local assets", e);
        }
    }

    private String resolveIconUrl(UUID assetId, JsonNode weapon) {
        String displayIcon = weapon.path("displayIcon").asText(null);
        if (displayIcon != null && !displayIcon.isBlank()) {
            return assetsBaseUrl + "/tools_riot/valorant/img/weapons/" + assetId + "/displayicon.png";
        }
        return null;
    }

    private UUID parseUuid(String value) {
        return (value != null && !value.isBlank()) ? UUID.fromString(value) : null;
    }
}
