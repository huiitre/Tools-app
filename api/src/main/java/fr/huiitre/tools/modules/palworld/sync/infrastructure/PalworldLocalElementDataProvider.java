package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ElementDataProvider;

public class PalworldLocalElementDataProvider implements ElementDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalElementDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public List<ElementSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("elements.json");
            JsonNode root = objectMapper.readTree(json);

            List<ElementSyncData> result = new ArrayList<>();
            for (JsonNode element : root) {
                String externalCode = element.path("id").asText(null);
                String name = element.path("name").asText(null);
                result.add(new ElementSyncData(externalCode, name, null));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld elements from local assets", e);
        }
    }
}
