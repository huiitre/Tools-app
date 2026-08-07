package fr.huiitre.tools.modules.palworld.sync.infrastructure;

import java.util.ArrayList;
import java.util.List;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.palworld.sync.application.MerchantOfferSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.MerchantSyncData;
import fr.huiitre.tools.modules.palworld.sync.application.ports.MerchantDataProvider;
import fr.huiitre.tools.modules.palworld.sync.application.ports.PalworldLanguageDataProvider;

public class PalworldLocalMerchantDataProvider implements MerchantDataProvider {

    private final PalworldLocalAssetsReader assetsReader;
    private final PalworldLanguageDataProvider languageDataProvider;
    private final String assetsBaseUrl;
    private final ObjectMapper objectMapper = new ObjectMapper();

    public PalworldLocalMerchantDataProvider(
            PalworldLocalAssetsReader assetsReader, PalworldLanguageDataProvider languageDataProvider, String assetsBaseUrl) {
        this.assetsReader = assetsReader;
        this.languageDataProvider = languageDataProvider;
        this.assetsBaseUrl = assetsBaseUrl;
    }

    @Override
    public List<MerchantSyncData> fetchAll() {
        try {
            JsonNode root = objectMapper.readTree(assetsReader.readFile("merchants.json"));
            List<MerchantSyncData> result = new ArrayList<>();
            for (JsonNode merchant : root) {
                String nameStringId = merchant.path("nameStringId").asText(null);
                String portrait = merchant.path("portrait").isNull() || merchant.path("portrait").isMissingNode()
                        ? null
                        : assetsBaseUrl + "/tools_palworld/palworld/" + merchant.path("portrait").asText();
                result.add(new MerchantSyncData(
                        merchant.path("id").asText(null),
                        merchant.path("code").asText(null),
                        nameStringId == null ? null : languageDataProvider.getString(nameStringId),
                        portrait,
                        intOrNull(merchant.path("restockMinute")),
                        merchant.path("currencyItemId").asText(null),
                        offers(merchant.path("offers"))));
            }
            return result;
        } catch (Exception e) {
            throw new IllegalStateException("Failed to load Palworld merchants from local assets", e);
        }
    }

    private List<MerchantOfferSyncData> offers(JsonNode node) {
        List<MerchantOfferSyncData> result = new ArrayList<>();
        for (JsonNode offer : node) {
            // Source: "Normal" | "OnlyPurchaseOne" -> normalisé vers le CHECK constraint DB
            // ('NORMAL' | 'ONLY_PURCHASE_ONE'), cf. V2.63.0__palworld_shop.sql.
            String productType = toSnakeUpper(offer.path("productType").asText(null));
            result.add(new MerchantOfferSyncData(
                    offer.path("itemId").asText(null),
                    offer.path("price").asInt(),
                    offer.path("quantityPerPurchase").asInt(1),
                    productType));
        }
        return result;
    }

    private String toSnakeUpper(String camel) {
        if (camel == null) return null;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < camel.length(); i++) {
            char c = camel.charAt(i);
            if (Character.isUpperCase(c) && i > 0) sb.append('_');
            sb.append(Character.toUpperCase(c));
        }
        return sb.toString();
    }

    private Integer intOrNull(JsonNode node) {
        return node.isMissingNode() || node.isNull() ? null : node.asInt();
    }
}
