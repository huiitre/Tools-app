package fr.huiitre.tools.modules.dofus.workshop.application.repository;

import java.util.Collection;
import java.util.List;
import java.util.Map;

import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

// Réalignement des ateliers sur les recettes après une synchro : parcourt les ateliers de tous les
// utilisateurs d'une version de jeu, d'où un port distinct de WorkshopRepository (limité à un utilisateur).
public interface WorkshopRecipeAlignmentRepository {

    record WorkshopItemRef(Long workshopItemId, Long workshopId, String workshopName, Long itemId) {}

    List<WorkshopItemRef> findAllWorkshopItems(Long gameVersionId);

    List<WorkshopItemIngredient> findAllIngredients(Long gameVersionId);

    Map<Long, String> findItemNames(Collection<Long> itemIds);

    void deleteIngredients(List<Long> ingredientIds);

    void insertIngredients(List<WorkshopItemIngredient> ingredients);
}
