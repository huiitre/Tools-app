package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.recipe.application.ports.RecipeRepository;
import fr.huiitre.tools.modules.dofus.recipe.domain.Recipe;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItem;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

@Service
@Transactional
public class UpdateWorkshopItemQuantityUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
    private final RecipeRepository recipeRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public UpdateWorkshopItemQuantityUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository,
        RecipeRepository recipeRepository
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.recipeRepository = recipeRepository;
    }

    public void execute(Long workshopId, Long workshopItemId, Long quantity) {
        Long userId = authenticatedUserProvider.getUserId();

        if (quantity <= 0) {
            throw new IllegalArgumentException("Quantity must be positive");
        }

        boolean exists = workshopRepository.existsByIdAndUserId(userId, workshopId);
        if (!exists) {
            throw new WorkshopNotFoundException();
        }

        List<WorkshopItem> items = workshopRepository.findAllItemsByUserIdAndWorkshopId(userId, workshopId);
        WorkshopItem item = items.stream()
            .filter(i -> i.getId().equals(workshopItemId))
            .findFirst()
            .orElseThrow();
        
        Long oldQuantity = item.getQuantity();
        
        workshopRepository.updateWorkshopItemQuantity(userId, workshopId, workshopItemId, quantity);

        if (oldQuantity.equals(quantity)) return;

        // Créer un item avec la nouvelle quantité pour les calculs
        WorkshopItem updatedItem = WorkshopItem.rehydrate(
            item.getId(),
            item.getItemId(),
            quantity
        );

        List<WorkshopItemIngredient> allIngredients = workshopRepository
            .findAllIngredientsByUserIdAndWorkshopItemId(userId, workshopItemId);
        
        Set<Long> allItemIds = new HashSet<>();
        allItemIds.add(item.getItemId());
        for (WorkshopItemIngredient ing : allIngredients) {
            allItemIds.add(ing.getItemId());
        }

        Map<Long, Map<Long, Long>> recipesByItemId = new HashMap<>();
        for (Long itemId : allItemIds) {
            List<Recipe> recipes = recipeRepository.findByItemId(itemId);
            Map<Long, Long> ingredientQuantities = new HashMap<>();
            for (Recipe recipe : recipes) {
                ingredientQuantities.put(recipe.getIngredientId(), recipe.getQuantity());
            }
            recipesByItemId.put(itemId, ingredientQuantities);
        }

        Map<Long, WorkshopItemIngredient> ingredientsById = new HashMap<>();
        for (WorkshopItemIngredient ing : allIngredients) {
            ingredientsById.put(ing.getId(), ing);
        }

        List<Long> craftedIds = allIngredients.stream()
            .filter(ing -> allIngredients.stream().anyMatch(sub -> sub.getParentIngredientId() != null && sub.getParentIngredientId().equals(ing.getId())))
            .map(WorkshopItemIngredient::getId)
            .toList();

        for (WorkshopItemIngredient ingredient : allIngredients) {
            if (ingredient.getQuantityObtained() == 0) continue;
            
            boolean isCrafted = craftedIds.contains(ingredient.getId());
            
            Long parentItemId;
            if (ingredient.getParentIngredientId() == null) {
                parentItemId = updatedItem.getItemId();
            } else {
                WorkshopItemIngredient parent = ingredientsById.get(ingredient.getParentIngredientId());
                parentItemId = parent.getItemId();
            }

            Long baseQuantityRequired = recipesByItemId
                .getOrDefault(parentItemId, Map.of())
                .getOrDefault(ingredient.getItemId(), 0L);

            if (isCrafted) {
                Long newQuantityObtained = (ingredient.getQuantityObtained() * quantity) / oldQuantity;
                workshopRepository.updateIngredientQuantityObtained(userId, ingredient.getId(), newQuantityObtained);
            } else {
                Long multiplier = calculateParentMultiplier(ingredient, ingredientsById, recipesByItemId, updatedItem);
                Long newQuantityRequired = baseQuantityRequired * multiplier;
                
                if (ingredient.getQuantityObtained() > newQuantityRequired) {
                    workshopRepository.updateIngredientQuantityObtained(userId, ingredient.getId(), newQuantityRequired);
                }
            }
        }
    }

    private Long calculateParentMultiplier(
        WorkshopItemIngredient ingredient,
        Map<Long, WorkshopItemIngredient> ingredientsById,
        Map<Long, Map<Long, Long>> recipesByItemId,
        WorkshopItem mainItem
    ) {
        if (ingredient.getParentIngredientId() == null) {
            return mainItem.getQuantity();
        }
        
        WorkshopItemIngredient parent = ingredientsById.get(ingredient.getParentIngredientId());
        Long parentMultiplier = calculateParentMultiplier(parent, ingredientsById, recipesByItemId, mainItem);
        
        // Déterminer l'item parent du parent
        Long parentItemId;
        if (parent.getParentIngredientId() == null) {
            parentItemId = mainItem.getItemId();
        } else {
            WorkshopItemIngredient grandParent = ingredientsById.get(parent.getParentIngredientId());
            parentItemId = grandParent.getItemId();
        }
        
        Long parentBaseQuantity = recipesByItemId
            .getOrDefault(parentItemId, Map.of())
            .getOrDefault(parent.getItemId(), 1L);
        
        return parentBaseQuantity * parentMultiplier;
    }
}