package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.core.logging.infrastructure.DebugLogger;
import fr.huiitre.tools.modules.dofus.sync.application.almanax.AlmanaxDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.almanax.AlmanaxSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

public class Dofus3AlmanaxDataProvider implements AlmanaxDataProvider {

    private final Dofus3LocalAssetsReader assetsReader;
    private final Dofus3LanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    private static final DebugLogger logger = DebugLogger.of(Dofus3AlmanaxDataProvider.class);

    public Dofus3AlmanaxDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    @Override
    public List<AlmanaxSyncData> fetchAll() {

        try {
            String almanaxJson = assetsReader.readFile("almanax.json");
            JsonNode almanaxRoot = objectMapper.readTree(almanaxJson);

            JsonNode almanaxRefIds = almanaxRoot
                    .path("references")
                    .path("RefIds");

            String objectiveJson = assetsReader.readFile("quest_objectives.json");
            JsonNode objectiveRoot = objectMapper.readTree(objectiveJson);

            JsonNode objectiveRefIds = objectiveRoot
                    .path("references")
                    .path("RefIds");

            Map<Long, JsonNode> objectiveById = new HashMap<>();
            for (JsonNode objRef : objectiveRefIds) {
                JsonNode objType = objRef.path("type");
                if (!"QuestObjectiveBringItemToNpcData".equals(objType.path("class").asText())) {
                    continue;
                }
                JsonNode objData = objRef.path("data");
                objectiveById.put(objData.path("id").asLong(), objData);
            }

            List<AlmanaxSyncData> result = new ArrayList<>();

            for (JsonNode ref : almanaxRefIds) {

                JsonNode type = ref.path("type");
                if (!"AlmanaxCalendarData".equals(type.path("class").asText())) {
                    continue;
                }

                JsonNode data = ref.path("data");

                Long assetId = data.path("id").asLong();
                String name = languageDataProvider.getString(data.path("nameId").asLong());
                String description = languageDataProvider.getString(data.path("descId").asLong());

                JsonNode dateArray = data.path("dates").path("Array");
                List<String> dates = new ArrayList<>();
                for (JsonNode dateNode : dateArray) {
                    dates.add(dateNode.asText());
                }

                Long itemId = null;
                Long itemQuantity = null;

                Long objectiveId = data.path("objectiveId").asLong();

                JsonNode objectiveData = objectiveById.get(objectiveId);
                if (objectiveData != null) {
                    JsonNode parameters = objectiveData.path("parameters");
                    itemId = parameters.path("parameter1").asLong();
                    itemQuantity = parameters.path("parameter2").asLong();
                }

                result.add(new AlmanaxSyncData(
                        assetId,
                        name,
                        description,
                        dates,
                        itemId,
                        itemQuantity));
            }

            return result;

        } catch (Exception e) {
            throw new RuntimeException("Erreur lors de la lecture des données Almanax Dofus3", e);
        }
    }
}
