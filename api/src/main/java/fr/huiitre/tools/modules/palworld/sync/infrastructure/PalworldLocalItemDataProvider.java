package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.ItemSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.ItemDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;

public class PalworldLocalItemDataProvider implements ItemDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final PalworldLanguageDataProvider languageDataProvider;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalItemDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    // Source de vérité complète du catalogue (items.json, ~2466 items, array plat déjà résolu par l'extracteur :
    // id/nameStringId/icon/price/maxStackCount — plus l'ancien format "Rows" bruts du datatable). Ne rien filtrer
    // ici (pas de flag fiable pour distinguer "vrai item" de ligne technique sans le deviner) : toutes les
    // entrées sont synchronisées telles quelles, name/icon restent transparents (via getString/null) quand la
    // donnée n'existe pas côté jeu plutôt que d'inventer une valeur. Pas de champ catégorie côté extracteur
    // depuis ce format : ItemSyncData.category() reste null pour tous les items.
    @Override
    public List<ItemSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("items.json"));
            List<ItemSyncData> result = new ArrayList<>();
            for (JsonNode item : root) {
                String icon = item.path("icon").asText(null);
                result.add(new ItemSyncData(
                        item.path("id").asText(null),
                        languageDataProvider.getString(item.path("nameStringId").asText(null)),
                        icon != null ? assetsBaseUrl + "/tools_palworld/palworld/" + icon : null,
                        intOrNull(item.path("price")),
                        intOrNull(item.path("maxStackCount")),
                        null));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld items from local assets", e);
        }
    }

    private Integer intOrNull(JsonNode node) {
        return node.isMissingNode() || node.isNull() ? null : node.asInt();
    }
}
