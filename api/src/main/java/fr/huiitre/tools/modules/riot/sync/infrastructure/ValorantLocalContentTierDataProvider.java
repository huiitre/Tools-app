package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.riot.sync.application.ValorantContentTierSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantContentTierDataProvider;

public class ValorantLocalContentTierDataProvider implements ValorantContentTierDataProvider {

    private final ValorantLocalAssetsReader assetsReader;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public ValorantLocalContentTierDataProvider(ValorantLocalAssetsReader assetsReader, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<ValorantContentTierSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("contenttiers.json");
            JsonNode root = objectMapper.readTree(json);
            JsonNode data = root.path("data");

            List<ValorantContentTierSyncData> result = new ArrayList<>();

            for (JsonNode tier : data) {
                UUID assetId = UUID.fromString(tier.get("uuid").asText());
                String name = tier.path("displayName").asText(null);
                String devName = tier.path("devName").asText(null);
                int rank = tier.path("rank").asInt(0);
                int juiceValue = tier.path("juiceValue").asInt(0);
                int juiceCost = tier.path("juiceCost").asInt(0);
                String highlightColor = tier.path("highlightColor").asText(null);
                String displayIconUrl = assetsBaseUrl + "/tools_riot/valorant/img/contenttiers/" + assetId + "/displayicon.png";

                result.add(new ValorantContentTierSyncData(assetId, name, devName, rank, juiceValue, juiceCost, highlightColor, displayIconUrl));
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Valorant content tiers from local assets", e);
        }
    }
}