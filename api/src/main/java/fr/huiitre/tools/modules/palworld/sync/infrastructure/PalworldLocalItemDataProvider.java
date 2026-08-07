package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

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

    // Source de vérité complète du catalogue (item_data.json, ~2466 lignes brutes du jeu), contrairement à
    // shop_items.json qui n'est qu'un sous-ensemble pré-filtré par l'extracteur pour les 267 items vendus.
    // Ne rien filtrer ici (pas de flag fiable pour distinguer "vrai item" de ligne technique sans le deviner) :
    // toutes les lignes sont synchronisées telles quelles, name/icon restent transparents (via getString/null)
    // quand la donnée n'existe pas côté jeu plutôt que d'inventer une valeur.
    @Override
    public List<ItemSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("item_data.json"));
            JsonNode rows = root.isArray() ? root.get(0).path("Rows") : root.path("Rows");

            // Mêmes réserves de casse que pour les images de Pals (cf. PalworldLocalPalDataProvider) :
            // les fichiers d'icônes ne sont pas garantis à la casse exacte du slug de l'item.
            Map<String, String> itemImageFileNameBySlugUpper = assetsReader.preferredImageFileNameByBaseName("item").entrySet().stream()
                    .collect(Collectors.toMap(e -> e.getKey().toUpperCase(), Map.Entry::getValue, (a, b) -> a));

            List<ItemSyncData> result = new ArrayList<>();
            Iterator<Map.Entry<String, JsonNode>> fields = rows.fields();
            while (fields.hasNext()) {
                Map.Entry<String, JsonNode> entry = fields.next();
                String slug = entry.getKey();
                JsonNode item = entry.getValue();
                result.add(new ItemSyncData(
                        slug,
                        languageDataProvider.getString("ITEM_NAME_" + slug),
                        resolveIconUrl(slug, item.path("IconName").asText(null), itemImageFileNameBySlugUpper),
                        intOrNull(item.path("Price")),
                        intOrNull(item.path("MaxStackCount")),
                        category(item.path("TypeA").asText(null))));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld items from local assets", e);
        }
    }

    private static final String TYPE_A_PREFIX = "EPalItemTypeA::";

    private String category(String typeA) {
        if (typeA == null) return null;
        return typeA.startsWith(TYPE_A_PREFIX) ? typeA.substring(TYPE_A_PREFIX.length()) : typeA;
    }

    // Le nom de fichier d'icône ne correspond pas toujours au slug de la ligne (23 cas sur 2466, vérifié) :
    // item_data.json fournit un champ IconName dédié, parfois différent (ex: variantes d'un même modèle
    // visuel). On essaie le slug d'abord (cas très majoritaire), IconName en repli.
    private String resolveIconUrl(String slug, String iconName, Map<String, String> itemImageFileNameBySlugUpper) {
        String fileName = itemImageFileNameBySlugUpper.get(slug.toUpperCase());
        if (fileName == null && iconName != null && !iconName.equals("None")) {
            fileName = itemImageFileNameBySlugUpper.get(iconName.toUpperCase());
        }
        return fileName != null ? assetsBaseUrl + "/tools_palworld/palworld/img/item/" + fileName : null;
    }

    private Integer intOrNull(JsonNode node) {
        return node.isMissingNode() || node.isNull() ? null : node.asInt();
    }
}
