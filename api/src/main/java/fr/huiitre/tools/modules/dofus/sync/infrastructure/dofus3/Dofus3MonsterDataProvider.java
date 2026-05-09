package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.monster.MonsterDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.monster.MonsterSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

public class Dofus3MonsterDataProvider implements MonsterDataProvider {
    
    private final Dofus3LocalAssetsReader assetsReader;
    private final Dofus3LanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public Dofus3MonsterDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    @Override
    public List<MonsterSyncData> fetchAll() {

        try {

            String json = assetsReader.readFile("monsters.json");
            JsonNode root = objectMapper.readTree(json);

            JsonNode refIds = root
                    .path("references")
                    .path("RefIds");

            List<MonsterSyncData> result = new ArrayList<>();

            for (JsonNode ref : refIds) {

                JsonNode type = ref.path("type");
                JsonNode classitem = type.path("class");

                if (!"MonsterData".equals(classitem.asText())) {
                    // * ignore les autres types
                    continue;
                }

                JsonNode data = ref.path("data");

                JsonNode drops = data.path("drops").path("Array");
                List<Long> dropItemIds = new ArrayList<>();
                for (JsonNode drop : drops) {
                    Long itemId = drop.path("objectId").asLong();
                    dropItemIds.add(itemId);
                }

                JsonNode subareas = data.path("subareas").path("Array");
                List<Long> subareaIds = new ArrayList<>();
                for (JsonNode subarea : subareas) {
                    Long subareaId = subarea.asLong();
                    subareaIds.add(subareaId);
                }

                Long assetId = data.path("id").asLong();
                Long nameId = data.path("nameId").asLong();
                String name = languageDataProvider.getString(nameId);
                Long iconId = data.path("gfxId").asLong();



                MonsterSyncData monsterSyncData = new MonsterSyncData(
                    assetId,
                    name,
                    iconId,
                    subareaIds,
                    dropItemIds
                );

                result.add(monsterSyncData);
            }

            return result;

        } catch(Exception e) {
            throw new IllegalStateException(
                "Failed to load Dofus3 monster file (monsters.json)",
                e);
        }
    }
}
