package fr.huiitre.tools.modules.riot.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.riot.sync.application.ValorantBundleSyncData;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantBundleDataProvider;

public class ValorantLocalBundleDataProvider implements ValorantBundleDataProvider {

    private final ValorantLocalAssetsReader assetsReader;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public ValorantLocalBundleDataProvider(ValorantLocalAssetsReader assetsReader, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<ValorantBundleSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("bundles.json");
            JsonNode root = objectMapper.readTree(json);
            JsonNode data = root.path("data");

            List<ValorantBundleSyncData> result = new ArrayList<>();

            for (JsonNode bundle : data) {
                UUID assetId = UUID.fromString(bundle.get("uuid").asText());
                String name = bundle.path("displayName").asText(null);
                String bannerUrl = resolveBannerUrl(assetId, bundle);

                result.add(new ValorantBundleSyncData(assetId, name, bannerUrl));
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Valorant bundles from local assets", e);
        }
    }

    private String resolveBannerUrl(UUID assetId, JsonNode bundle) {
        String displayIcon2 = bundle.path("displayIcon2").asText(null);
        if (displayIcon2 != null && !displayIcon2.isBlank()) {
            return assetsBaseUrl + "/tools_riot/valorant/img/bundles/" + assetId + "/displayicon2.png";
        }
        return null;
    }
}
