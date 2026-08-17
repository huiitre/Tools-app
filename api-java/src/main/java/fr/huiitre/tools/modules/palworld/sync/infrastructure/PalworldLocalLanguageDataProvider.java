package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;

public class PalworldLocalLanguageDataProvider implements PalworldLanguageDataProvider {

    private final Map<String, String> stringsById;
    private final Map<String, String> descriptionsById;

    public PalworldLocalLanguageDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.stringsById = loadFlatMap(assetsReader, "strings.json");
        this.descriptionsById = loadFlatMap(assetsReader, "strings_descriptions.json");
    }

    @Override
    public String getString(String stringId) {
        if (stringId == null) return null;
        return stringsById.getOrDefault(stringId, "[missing-string:" + stringId + "]");
    }

    @Override
    public String getDescription(String stringId) {
        if (stringId == null) return null;
        return descriptionsById.getOrDefault(stringId, "[missing-string:" + stringId + "]");
    }

    private Map<String, String> loadFlatMap(PalworldLocalAssetsReader assetsReader, String fileName) {
        try {
            JsonNode root = new ObjectMapper().readTree(assetsReader.readFile(fileName));
            Map<String, String> map = new HashMap<>();
            Iterator<Map.Entry<String, JsonNode>> fields = root.fields();
            while (fields.hasNext()) {
                Map.Entry<String, JsonNode> entry = fields.next();
                map.put(entry.getKey(), entry.getValue().asText());
            }
            return map;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld language file: " + fileName, e);
        }
    }
}
