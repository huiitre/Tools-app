package fr.huiitre.tools.modules.dofus.sync.application.recipe;

import java.util.List;

public interface RecipeDataProvider {

    boolean supports(String gameVersionCode);
    List<RecipeSyncData> fetchAll();
}
