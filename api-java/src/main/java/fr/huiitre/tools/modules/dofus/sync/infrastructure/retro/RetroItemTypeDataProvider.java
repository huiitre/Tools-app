package fr.huiitre.tools.modules.dofus.sync.infrastructure.retro;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

import org.springframework.stereotype.Component;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.itemtype.ItemTypeDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.itemtype.ItemTypeSyncData;

@Component
public class RetroItemTypeDataProvider implements ItemTypeDataProvider {

    private final RetroLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public RetroItemTypeDataProvider(RetroLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public boolean supports(String gameVersionCode) {
        return "retro".equals(gameVersionCode);
    }

    @Override
    public List<ItemTypeSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("items.json");
            JsonNode root = objectMapper.readTree(json);
            JsonNode typesNode = root.path("t"); // La clé 't' contient les types dans Retro

            List<ItemTypeSyncData> result = new ArrayList<>();
            Iterator<Map.Entry<String, JsonNode>> fields = typesNode.fields();

            while (fields.hasNext()) {
                Map.Entry<String, JsonNode> entry = fields.next();
                long assetId = Long.parseLong(entry.getKey());
                JsonNode typeNode = entry.getValue();
                String name = typeNode.path("n").asText();
                long categoryId = 0L;

                result.add(new ItemTypeSyncData(assetId, name, categoryId));
            }

            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load item types from Retro assets", e);
        }
    }
}