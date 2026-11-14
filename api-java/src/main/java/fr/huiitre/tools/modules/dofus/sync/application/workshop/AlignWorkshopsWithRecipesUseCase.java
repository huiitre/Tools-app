package fr.huiitre.tools.modules.dofus.sync.application.workshop;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.recipe.application.ports.RecipeRepository;
import fr.huiitre.tools.modules.dofus.recipe.domain.Recipe;
import fr.huiitre.tools.modules.dofus.sync.application.sync.usecase.SyncReport;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRecipeAlignmentRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRecipeAlignmentRepository.WorkshopItemRef;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

// Les ateliers copient les recettes au moment de l'ajout d'un objet : après une synchro qui change une
// recette, ils gardent l'ancienne. On réaligne chaque niveau existant (ingrédients directs d'un objet, et
// ceux d'un ingrédient que l'utilisateur a choisi de crafter) sur la recette actuelle, sans créer de
// nouveau niveau : crafter un ingrédient reste un choix de l'utilisateur. Les quantités obtenues des
// ingrédients conservés ne bougent pas.
@Service
@Transactional
public class AlignWorkshopsWithRecipesUseCase implements SecuredUseCase {

    private static final Logger logger = LoggerFactory.getLogger(AlignWorkshopsWithRecipesUseCase.class);

    private final WorkshopRecipeAlignmentRepository alignmentRepository;
    private final RecipeRepository recipeRepository;

    public AlignWorkshopsWithRecipesUseCase(
        WorkshopRecipeAlignmentRepository alignmentRepository,
        RecipeRepository recipeRepository) {
        this.alignmentRepository = alignmentRepository;
        this.recipeRepository = recipeRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    private record Change(WorkshopItemRef workshopItem, Long parentItemId, Set<Long> removed, Set<Long> added) {}

    public SyncReport execute(GameVersionData gameVersion) {
        List<WorkshopItemRef> workshopItems = alignmentRepository.findAllWorkshopItems(gameVersion.getId());
        List<WorkshopItemIngredient> ingredients = alignmentRepository.findAllIngredients(gameVersion.getId());

        Map<Long, WorkshopItemIngredient> ingredientsById = new HashMap<>();
        Map<Long, List<WorkshopItemIngredient>> rootsByWorkshopItem = new HashMap<>();
        Map<Long, List<WorkshopItemIngredient>> childrenByParent = new LinkedHashMap<>();
        for (WorkshopItemIngredient ingredient : ingredients) {
            ingredientsById.put(ingredient.getId(), ingredient);
            if (ingredient.getParentIngredientId() == null) {
                rootsByWorkshopItem.computeIfAbsent(ingredient.getWorkshopItemId(), k -> new ArrayList<>()).add(ingredient);
            } else {
                childrenByParent.computeIfAbsent(ingredient.getParentIngredientId(), k -> new ArrayList<>()).add(ingredient);
            }
        }

        Map<Long, Set<Long>> recipeCache = new HashMap<>();
        Set<Long> deleted = new HashSet<>();
        List<WorkshopItemIngredient> toInsert = new ArrayList<>();
        List<Change> changes = new ArrayList<>();

        for (WorkshopItemRef workshopItem : workshopItems) {
            List<WorkshopItemIngredient> roots = rootsByWorkshopItem.getOrDefault(workshopItem.workshopItemId(), List.of());
            alignLevel(workshopItem, null, workshopItem.itemId(), roots, recipeCache, deleted, toInsert, changes);
        }

        // Niveaux craftés, du moins profond au plus profond : un niveau sous un ingrédient supprimé est ignoré.
        Map<Long, WorkshopItemRef> workshopItemsById = workshopItems.stream()
            .collect(Collectors.toMap(WorkshopItemRef::workshopItemId, w -> w));
        List<Map.Entry<Long, List<WorkshopItemIngredient>>> levels = new ArrayList<>(childrenByParent.entrySet());
        levels.sort((a, b) -> Integer.compare(
            depth(ingredientsById.get(a.getKey()), ingredientsById),
            depth(ingredientsById.get(b.getKey()), ingredientsById)));
        for (Map.Entry<Long, List<WorkshopItemIngredient>> level : levels) {
            WorkshopItemIngredient parent = ingredientsById.get(level.getKey());
            if (parent == null || isDeleted(parent, ingredientsById, deleted)) {
                continue;
            }
            WorkshopItemRef workshopItem = workshopItemsById.get(parent.getWorkshopItemId());
            alignLevel(workshopItem, parent.getId(), parent.getItemId(), level.getValue(), recipeCache, deleted, toInsert, changes);
        }

        alignmentRepository.deleteIngredients(new ArrayList<>(deleted));
        alignmentRepository.insertIngredients(toInsert);

        List<String> updated = describe(changes);
        logger.info("Ateliers réalignés sur les recettes : {} niveau(x) modifié(s), {} ingrédient(s) supprimé(s), {} ajouté(s)",
            changes.size(), deleted.size(), toInsert.size());

        return new SyncReport("workshops", "Ateliers", List.of(), updated);
    }

    private void alignLevel(
        WorkshopItemRef workshopItem,
        Long parentIngredientId,
        Long parentItemId,
        List<WorkshopItemIngredient> children,
        Map<Long, Set<Long>> recipeCache,
        Set<Long> deleted,
        List<WorkshopItemIngredient> toInsert,
        List<Change> changes) {

        Set<Long> recipe = recipeCache.computeIfAbsent(parentItemId, id -> recipeRepository.findByItemId(id).stream()
            .map(Recipe::getIngredientId)
            .collect(Collectors.toCollection(LinkedHashSet::new)));

        Set<Long> removed = new LinkedHashSet<>();
        Set<Long> present = new HashSet<>();
        for (WorkshopItemIngredient child : children) {
            if (recipe.contains(child.getItemId())) {
                present.add(child.getItemId());
            } else {
                deleted.add(child.getId());
                removed.add(child.getItemId());
            }
        }

        Set<Long> added = new LinkedHashSet<>();
        for (Long ingredientId : recipe) {
            if (!present.contains(ingredientId)) {
                toInsert.add(WorkshopItemIngredient.create(workshopItem.workshopItemId(), ingredientId, parentIngredientId, 0L));
                added.add(ingredientId);
            }
        }

        if (!removed.isEmpty() || !added.isEmpty()) {
            changes.add(new Change(workshopItem, parentItemId, removed, added));
        }
    }

    private int depth(WorkshopItemIngredient ingredient, Map<Long, WorkshopItemIngredient> ingredientsById) {
        int depth = 0;
        WorkshopItemIngredient current = ingredient;
        while (current != null && current.getParentIngredientId() != null) {
            depth++;
            current = ingredientsById.get(current.getParentIngredientId());
        }
        return depth;
    }

    private boolean isDeleted(WorkshopItemIngredient ingredient, Map<Long, WorkshopItemIngredient> ingredientsById, Set<Long> deleted) {
        WorkshopItemIngredient current = ingredient;
        while (current != null) {
            if (deleted.contains(current.getId())) {
                return true;
            }
            current = current.getParentIngredientId() == null ? null : ingredientsById.get(current.getParentIngredientId());
        }
        return false;
    }

    private List<String> describe(List<Change> changes) {
        Set<Long> itemIds = new HashSet<>();
        for (Change change : changes) {
            itemIds.add(change.parentItemId());
            itemIds.addAll(change.removed());
            itemIds.addAll(change.added());
        }
        Map<Long, String> names = alignmentRepository.findItemNames(itemIds);

        List<String> lines = new ArrayList<>();
        for (Change change : changes) {
            StringBuilder line = new StringBuilder()
                .append("atelier=\"").append(change.workshopItem().workshopName()).append("\" (#")
                .append(change.workshopItem().workshopId()).append(") ")
                .append(names.getOrDefault(change.parentItemId(), "#" + change.parentItemId())).append(" :");
            change.removed().forEach(id -> line.append(" - ").append(names.getOrDefault(id, "#" + id)));
            change.added().forEach(id -> line.append(" + ").append(names.getOrDefault(id, "#" + id)));
            lines.add(line.toString());
        }
        return lines;
    }
}
