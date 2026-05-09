package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.subarea.SubAreaDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.subarea.SubareaSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

public class Dofus3SubareaDataProvider implements SubAreaDataProvider {
    
    private final Dofus3LocalAssetsReader assetsReader;
    private final Dofus3LanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public Dofus3SubareaDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    @Override
    public List<SubareaSyncData> fetchAll() {
        
        try {

            String json = assetsReader.readFile("subareas.json");
            JsonNode root = objectMapper.readTree(json);

            JsonNode refIds = root
                    .path("references")
                    .path("RefIds");

            List<SubareaSyncData> result = new ArrayList<>();

            for (JsonNode ref : refIds) {

                JsonNode type = ref.path("type");
                JsonNode classitem = type.path("class");

                if (!"SubAreaData".equals(classitem.asText())) {
                    // * ignore les autres types
                    continue;
                }

                JsonNode data = ref.path("data");

                Long assetId = data.path("id").asLong();
                Long areaId = data.path("areaId").asLong();
                Long nameId = data.path("nameId").asLong();
                String name = languageDataProvider.getString(nameId);

                SubareaSyncData subareaSyncData = new SubareaSyncData(
                    assetId,
                    areaId,
                    name);

                result.add(subareaSyncData);
            }

            return result;

        } catch(Exception e) {
            throw new IllegalStateException(
                "Failed to load Dofus3 subareas file (subareas.json)",
                e);
        }
    }
}
