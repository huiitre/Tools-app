package fr.huiitre.tools.modules.riot.valorant.infrastructure;

import java.util.Map;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.riot.sync.infrastructure.ValorantLocalAssetsReader;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantVersionProvider;

public class ValorantLocalVersionProvider implements ValorantVersionProvider {

    private final ValorantLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public ValorantLocalVersionProvider(ValorantLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    @SuppressWarnings("unchecked")
    public Map<String, Object> getVersion() {
        try {
            String json = assetsReader.readFile("version.json");
            Map<String, Object> root = objectMapper.readValue(json, new TypeReference<Map<String, Object>>() {});
            return (Map<String, Object>) root.get("data");
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Valorant version from local assets", e);
        }
    }
}
