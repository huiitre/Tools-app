package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.BreedingExceptionSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.BreedingExceptionDataProvider;

public class PalworldLocalBreedingExceptionDataProvider implements BreedingExceptionDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalBreedingExceptionDataProvider(PalworldLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public List<BreedingExceptionSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("breeding.json"));
            List<BreedingExceptionSyncData> result = new ArrayList<>();
            for (JsonNode entry : root) {
                JsonNode parentA = entry.path("parentA");
                JsonNode parentB = entry.path("parentB");
                result.add(new BreedingExceptionSyncData(
                        parentA.path("tribe").asText(null),
                        parentA.path("gender").asText(null),
                        parentB.path("tribe").asText(null),
                        parentB.path("gender").asText(null),
                        entry.path("child").asText(null)));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld breeding exceptions from local assets", e);
        }
    }
}
