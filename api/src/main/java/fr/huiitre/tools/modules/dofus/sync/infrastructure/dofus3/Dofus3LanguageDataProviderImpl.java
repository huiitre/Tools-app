package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

public class Dofus3LanguageDataProviderImpl implements Dofus3LanguageDataProvider {

    private final Dofus3LocalAssetsReader assetsReader;
    private volatile Map<Long, String> stringsById;

    public Dofus3LanguageDataProviderImpl(
            Dofus3LocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
        this.stringsById = loadStrings(assetsReader);
    }

    @Override
    public String getString(Long stringId) {
        return stringsById.getOrDefault(
                stringId,
                "[missing-string:" + stringId + "]");
    }

    @Override
    public synchronized void reload() {
        this.stringsById = loadStrings(this.assetsReader);
    }

    private Map<Long, String> loadStrings(
            Dofus3LocalAssetsReader assetsReader) {
        try {
            String json = assetsReader.readFile("languages/fr.json");

            ObjectMapper mapper = new ObjectMapper();
            JsonNode root = mapper.readTree(json);

            JsonNode entries = root.path("entries");

            Map<Long, String> map = new HashMap<>();

            Iterator<Map.Entry<String, JsonNode>> fields = entries.fields();
            while (fields.hasNext()) {
                Map.Entry<String, JsonNode> entry = fields.next();
                Long id = Long.valueOf(entry.getKey());
                String value = entry.getValue().asText();
                map.put(id, value);
            }

            return map;

        } catch (Exception e) {
            throw new IllegalStateException(
                    "Failed to load Dofus3 language file (fr.json)",
                    e);
        }
    }
}
