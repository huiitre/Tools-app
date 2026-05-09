package fr.huiitre.tools.modules.dofus.sync.infrastructure.retro;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

import org.springframework.stereotype.Component;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.item.ItemDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.item.ItemSyncData;

@Component
public class RetroItemDataProvider implements ItemDataProvider {

    private final RetroLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public RetroItemDataProvider(RetroLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public boolean supports(String gameVersionCode) {
        return "retro".equals(gameVersionCode);
    }

    @Override
    public List<ItemSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("items.json");
            JsonNode root = objectMapper.readTree(json);
            JsonNode itemsNode = root.path("u"); // La clé 'u' contient les items dans Retro

            List<ItemSyncData> result = new ArrayList<>();
            Iterator<Map.Entry<String, JsonNode>> fields = itemsNode.fields();

            while (fields.hasNext()) {
                Map.Entry<String, JsonNode> entry = fields.next();
                JsonNode data = entry.getValue();

                long assetId = Long.parseLong(entry.getKey());
                String name = data.path("n").asText();
                long level = data.path("l").asLong();
                String description = data.path("d").asText();
                long itemTypeId = data.path("t").asLong();
                long iconId = assetId;

                result.add(new ItemSyncData(
                        assetId,
                        name,
                        level,
                        description,
                        itemTypeId,
                        iconId
                ));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load items from Retro assets", e);
        }
    }
}