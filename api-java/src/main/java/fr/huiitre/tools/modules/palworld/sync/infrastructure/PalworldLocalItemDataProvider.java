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
    // id/nameStringId/icon/price/maxStackCount/category — plus l'ancien format "Rows" bruts du datatable).
    // category est déjà dépréfixée côté extracteur (EPalItemTypeA:: retiré), aucun traitement à faire ici.
    // Ne rien filtrer (pas de flag fiable pour distinguer "vrai item" de ligne technique sans le deviner) :
    // toutes les entrées sont synchronisées telles quelles, icon reste transparent (null) quand la donnée
    // n'existe pas côté jeu.
    @Override
    public List<ItemSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("items.json"));
            List<ItemSyncData> result = new ArrayList<>();
            for (JsonNode item : root) {
                String slug = item.path("id").asText(null);
                String icon = item.path("icon").asText(null);
                String nameStringId = item.path("nameStringId").asText(null);
                // 623/2466 items n'ont pas de nameStringId (pas de traduction dans le jeu, extracteur renvoie
                // null) : "name" est NOT NULL en base, donc on retombe sur la même clé que l'ancien format
                // ("ITEM_NAME_" + slug) pour obtenir le placeholder transparent "[missing-string:...]" plutôt
                // qu'un null qui fait échouer l'upsert.
                result.add(new ItemSyncData(
                        slug,
                        languageDataProvider.getString(nameStringId != null ? nameStringId : "ITEM_NAME_" + slug),
                        icon != null ? assetsBaseUrl + "/tools_palworld/palworld/" + icon : null,
                        intOrNull(item.path("price")),
                        intOrNull(item.path("maxStackCount")),
                        item.path("category").asText(null)));
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
