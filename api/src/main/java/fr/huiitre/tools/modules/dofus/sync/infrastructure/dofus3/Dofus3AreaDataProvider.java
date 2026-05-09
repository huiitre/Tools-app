package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.area.AreaDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.area.AreaSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

public class Dofus3AreaDataProvider implements AreaDataProvider {
    
    private final Dofus3LocalAssetsReader assetsReader;
    private final Dofus3LanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public Dofus3AreaDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    @Override
    public List<AreaSyncData> fetchAll() {
        
        try {

            String json = assetsReader.readFile("areas.json");
            JsonNode root = objectMapper.readTree(json);

            JsonNode refIds = root
                    .path("references")
                    .path("RefIds");

            List<AreaSyncData> result = new ArrayList<>();

            for (JsonNode ref : refIds) {

                JsonNode type = ref.path("type");
                JsonNode classitem = type.path("class");

                if (!"AreaData".equals(classitem.asText())) {
                    // * ignore les autres types
                    continue;
                }

                JsonNode data = ref.path("data");

                Long assetId = data.path("id").asLong();
                Long nameId = data.path("nameId").asLong();
                String name = languageDataProvider.getString(nameId);

                AreaSyncData areaSyncData = new AreaSyncData(
                    assetId,
                    name);

                result.add(areaSyncData);
            }

            return result;

        } catch(Exception e) {
            throw new IllegalStateException(
            "Failed to load Dofus3 area data",
                e);
        }
    }
}
