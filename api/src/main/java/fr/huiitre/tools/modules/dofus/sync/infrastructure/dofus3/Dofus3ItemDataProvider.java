package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.item.ItemDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.item.ItemSyncData;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

/**
 * Implémentation Dofus3 basée sur les assets locaux.
 *
 * Responsabilité :
 * - lire les fichiers locaux
 * - parser
 * - produire des ItemSyncData
 *
 * PAS de logique métier.
 */
public class Dofus3ItemDataProvider implements ItemDataProvider {

    private final Dofus3LocalAssetsReader assetsReader;
    private final Dofus3LanguageDataProvider languageDataProvider;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public Dofus3ItemDataProvider(
            Dofus3LocalAssetsReader assetsReader,
            Dofus3LanguageDataProvider languageDataProvider) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
    }

    @Override
    public boolean supports(String gameVersionCode) {
        return "dofus3".equals(gameVersionCode);
    }

    @Override
    public List<ItemSyncData> fetchAll() {

        try {
            String json = assetsReader.readFile("items.json");
            JsonNode root = objectMapper.readTree(json);

            JsonNode refIds = root
                    .path("references")
                    .path("RefIds");

            List<ItemSyncData> result = new ArrayList<>();

            for (JsonNode ref : refIds) {

                JsonNode type = ref.path("type");
                JsonNode classitem = type.path("class");

                if ("EffectInstanceDice".equals(classitem.asText())) {
                    // * ignore les effets
                    continue;
                }

                JsonNode data = ref.path("data");

                long assetId = data.path("id").asLong();
                String name = languageDataProvider.getString(data.path("nameId").asLong());
                long level = data.path("level").asLong();
                String description = languageDataProvider.getString(data.path("descriptionId").asLong());
                long itemTypeId = data.path("typeId").asLong();
                long iconId = data.path("iconId").asLong();

                result.add(
                        new ItemSyncData(
                                assetId,
                                name,
                                level,
                                description,
                                itemTypeId,
                                iconId));
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException(
                    "Failed to load item types from Dofus3 local assets",
                    e);
        }
    }
}
