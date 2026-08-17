package fr.huiitre.tools.modules.palworld.tierlist.infrastructure;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalAssetsReader;
import fr.huiitre.tools.modules.palworld.tierlist.application.TierEntrySyncData;
import fr.huiitre.tools.modules.palworld.tierlist.application.TierListSourceSyncData;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.TierListDataProvider;

public class PalworldLocalTierListDataProvider implements TierListDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalTierListDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public List<TierListSourceSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("tierlists.json"));

            List<TierListSourceSyncData> result = new ArrayList<>();
            for (JsonNode sourceNode : root) {
                result.add(toSourceSyncData(sourceNode));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld tierlists from local assets", e);
        }
    }

    private TierListSourceSyncData toSourceSyncData(JsonNode sourceNode) {
        String source = sourceNode.path("source").asText(null);

        Map<String, List<TierEntrySyncData>> categories = new LinkedHashMap<>();
        Iterator<Map.Entry<String, JsonNode>> fields = sourceNode.path("categories").fields();
        while (fields.hasNext()) {
            Map.Entry<String, JsonNode> categoryField = fields.next();
            categories.put(categoryField.getKey(), entries(categoryField.getValue()));
        }

        return new TierListSourceSyncData(source, categories);
    }

    private List<TierEntrySyncData> entries(JsonNode categoryNode) {
        List<TierEntrySyncData> entries = new ArrayList<>();
        for (JsonNode palNode : categoryNode) {
            entries.add(new TierEntrySyncData(palNode.path("tier").asText(null), palNode.path("tribe").asText(null)));
        }
        return entries;
    }
}
