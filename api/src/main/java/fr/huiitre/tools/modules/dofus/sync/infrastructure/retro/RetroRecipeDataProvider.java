package fr.huiitre.tools.modules.dofus.sync.infrastructure.retro;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import fr.huiitre.tools.modules.dofus.sync.application.recipe.RecipeDataProvider;
import fr.huiitre.tools.modules.dofus.sync.application.recipe.RecipeSyncData;

@Component
public class RetroRecipeDataProvider implements RecipeDataProvider {

    private final RetroLocalAssetsReader assetsReader;
    private final ObjectMapper objectMapper = new ObjectMapper();

    private final static Logger logger = LoggerFactory.getLogger(RetroRecipeDataProvider.class);

    public RetroRecipeDataProvider(RetroLocalAssetsReader assetsReader) {
        this.assetsReader = assetsReader;
    }

    @Override
    public boolean supports(String gameVersionCode) {
        return "retro".equals(gameVersionCode);
    }

    @Override
    public List<RecipeSyncData> fetchAll() {
        try {
            String json = assetsReader.readFile("crafts.json");
            JsonNode root = objectMapper.readTree(json);
            JsonNode rootNode = root.path("_root");

            List<RecipeSyncData> result = new ArrayList<>();
            Iterator<Map.Entry<String, JsonNode>> fields = rootNode.fields();

            while (fields.hasNext()) {
                Map.Entry<String, JsonNode> entry = fields.next();
                long itemId = Long.parseLong(entry.getKey());
                JsonNode ingredientsArray = entry.getValue();

                if (!ingredientsArray.isArray()) {
                    logger.warn("Recette ignorée : format invalide pour itemId={}", itemId);
                    continue;
                }

                // Parcours du tableau de tableaux (ex: [ [757, 100], [368, 100] ])
                for (JsonNode ingredientPair : ingredientsArray) {
                    if (ingredientPair.isArray() && ingredientPair.size() == 2) {
                        long ingredientId = ingredientPair.get(0).asLong();
                        long quantity = ingredientPair.get(1).asLong();

                        result.add(new RecipeSyncData(itemId, ingredientId, quantity));
                    } else {
                        logger.warn("Paire d'ingrédients mal formée pour la recette de l'itemId={}", itemId);
                    }
                }
            }

            return result;

        } catch (Exception e) {
            throw new IllegalStateException("Erreur lors de la lecture des recettes Retro (crafts.json)", e);
        }
    }
}