package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.WorkPrioritySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.WorkPriorityDataProvider;

public class PalworldLocalWorkPriorityDataProvider implements WorkPriorityDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalWorkPriorityDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public List<WorkPrioritySyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("work_priority.json");
            JsonNode root = objectMapper.readTree(json);

            List<WorkPrioritySyncData> result = new ArrayList<>();
            for (JsonNode wp : root) {
                result.add(new WorkPrioritySyncData(
                        wp.path("code").asText(null),
                        wp.path("name").asText(null),
                        wp.path("icon").isNull() ? null : wp.path("icon").asText(null),
                        wp.path("workSuitabilitySlug").isNull() ? null : wp.path("workSuitabilitySlug").asText(null),
                        Integer.parseInt(wp.path("priority").asText())));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld work priorities from local assets", e);
        }
    }
}
