package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import java.util.ArrayList;
import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.recipe.RecipeDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.recipe.RecipeSyncData;

public class Dofus3RecipeDataProvider implements RecipeDataProvider {

    private final Dofus3LocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    private final static Logger logger = LoggerFactory.getLogger(Dofus3RecipeDataProvider.class);

    public Dofus3RecipeDataProvider(Dofus3LocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public boolean supports(String gameVersionCode) {
        return "dofus3".equals(gameVersionCode);
    }

    @Override
    public List<RecipeSyncData> fetchAll() {

        try {
            String json = assetsReader.readFile("recipes.json");
            JsonNode root = objectMapper.readTree(json);

            JsonNode refIds = root.path("references").path("RefIds");

            List<RecipeSyncData> result = new ArrayList<>();

            for (JsonNode ref : refIds) {

                JsonNode data = ref.path("data");

                long itemId = data.path("resultId").asLong();

                JsonNode ingredientArray = data.path("ingredientIds").path("Array");
                JsonNode quantityArray = data.path("quantities").path("Array");

                if (!ingredientArray.isArray() || !quantityArray.isArray()) {
                    throw new IllegalStateException("Recette invalide pour resultId=" + itemId);
                }

                if (ingredientArray.size() != quantityArray.size()) {
                    throw new IllegalStateException(
                            "Mismatch ingredientIds/quantities pour resultId=" + itemId
                                    + " (" + ingredientArray.size() + " vs " + quantityArray.size() + ")");
                }

                for (int i = 0; i < ingredientArray.size(); i++) {
                    long ingredientId = ingredientArray.get(i).asLong();
                    long quantity = quantityArray.get(i).asLong();

                    result.add(new RecipeSyncData(itemId, ingredientId, quantity));
                }
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException("Erreur lors de la lecture des recettes Dofus 3", e);
        }
    }
}
