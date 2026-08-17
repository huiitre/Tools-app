package fr.huiitre.tools.modules.dofus.recipe.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.recipe.domain.Recipe;

public interface RecipeRepository {
    
    void insert(Long itemId, Long ingredientId, Long quantity);

    void update(Long itemId, Long ingredientId, Long quantity);

    boolean exists(Long itemId, Long ingredientId);

    List<Recipe> findByItemId(Long itemId);

    void deleteByItemId(Long itemId);
}
