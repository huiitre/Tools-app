package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkSuitabilityDataProvider;

public class PalworldLocalWorkSuitabilityDataProvider implements WorkSuitabilityDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalWorkSuitabilityDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public List<WorkSuitabilitySyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("work_suitability.json");
            JsonNode root = objectMapper.readTree(json);

            List<WorkSuitabilitySyncData> result = new ArrayList<>();
            for (JsonNode ws : root) {
                result.add(new WorkSuitabilitySyncData(
                        ws.path("id").asText(null),
                        ws.path("slug").asText(null),
                        ws.path("name").asText(null),
                        ws.path("icon").isNull() ? null : ws.path("icon").asText(null)));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld work suitabilities from local assets", e);
        }
    }
}
