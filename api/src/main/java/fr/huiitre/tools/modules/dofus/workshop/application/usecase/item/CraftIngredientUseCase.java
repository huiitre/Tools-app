package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.dto.FarmZoneDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.application.service.ItemEnrichmentService;
import fr.huiitre.tools.modules.dofus.recipe.application.ports.RecipeRepository;
import fr.huiitre.tools.modules.dofus.recipe.domain.Recipe;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopIngredientDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItem;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

@Service
@Transactional
public class CraftIngredientUseCase implements SecuredUseCase {

    private final static Logger logger = LoggerFactory.getLogger(CraftIngredientUseCase.class);

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final WorkshopRepository workshopRepository;
    private final RecipeRepository recipeRepository;
    private final ItemRepository itemRepository;
    private final ItemEnrichmentService itemEnrichmentService;
    private final GameVersionRepository gameVersionRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.USER;
    }

    public CraftIngredientUseCase(
        AuthenticatedUserProvider authenticatedUserProvider,
        WorkshopRepository workshopRepository,
        RecipeRepository recipeRepository,
        ItemRepository itemRepository,
        ItemEnrichmentService itemEnrichmentService,
        GameVersionRepository gameVersionRepository
    ) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.recipeRepository = recipeRepository;
        this.itemRepository = itemRepository;
        this.itemEnrichmentService = itemEnrichmentService;
        this.gameVersionRepository = gameVersionRepository;
    }

    public List<WorkshopIngredientDetailDto> execute(Long workshopId, Long workshopItemId, Long ingredientId, Long gameVersionId) {
        Long userId = authenticatedUserProvider.getUserId();

        boolean exists = workshopRepository.existsByIdAndUserId(userId, workshopId);
        if (!exists) {
            throw new WorkshopNotFoundException();
        }

        Optional<WorkshopItemIngredient> parentIngredientOpt = workshopRepository.findIngredientByIdAndUserId(userId, ingredientId);
        if (parentIngredientOpt.isEmpty()) {
            throw new IllegalArgumentException("Ingredient not found");
        }

        WorkshopItemIngredient parentIngredient = parentIngredientOpt.get();
        Long itemIdToCraft = parentIngredient.getItemId();

        List<Recipe> recipes = recipeRepository.findByItemId(itemIdToCraft);
        List<WorkshopItemIngredient> ingredients = new ArrayList<>();

        for (Recipe recipe : recipes) {
            WorkshopItemIngredient ingredient = WorkshopItemIngredient.create(
                workshopItemId,
                recipe.getIngredientId(),
                ingredientId,
                0L
            );
            ingredients.add(ingredient);
        }

        workshopRepository.addIngredients(userId, ingredients);

        List<WorkshopItem> items = workshopRepository.findAllItemsByUserIdAndWorkshopId(userId, workshopId);
        WorkshopItem mainItem = items.stream()
            .filter(i -> i.getId().equals(workshopItemId))
            .findFirst()
            .orElseThrow();

        List<WorkshopItemIngredient> allIngredients = workshopRepository.findAllIngredientsByUserIdAndWorkshopItemId(userId, workshopItemId);
        Map<Long, WorkshopItemIngredient> ingredientsById = new HashMap<>();
        Set<Long> allItemIds = new HashSet<>();

        ingredientsById.put(parentIngredient.getId(), parentIngredient);
        allItemIds.add(parentIngredient.getItemId());
        allItemIds.add(mainItem.getItemId());
        for (WorkshopItemIngredient ing : allIngredients) {
            ingredientsById.put(ing.getId(), ing);
            allItemIds.add(ing.getItemId());
        }

        Map<Long, Map<Long, Long>> recipesByItemId = new HashMap<>();
        for (Long itemId : allItemIds) {
            List<Recipe> recipesForItem = recipeRepository.findByItemId(itemId);
            Map<Long, Long> ingredientQuantities = new HashMap<>();
            for (Recipe recipe : recipesForItem) {
                ingredientQuantities.put(recipe.getIngredientId(), recipe.getQuantity());
            }
            recipesByItemId.put(itemId, ingredientQuantities);
        }

        Long parentOfParentItemId;
        if (parentIngredient.getParentIngredientId() == null) {
            parentOfParentItemId = mainItem.getItemId();
        } else {
            WorkshopItemIngredient grandParent = ingredientsById.get(parentIngredient.getParentIngredientId());
            parentOfParentItemId = grandParent.getItemId();
        }

        Long baseQuantityRequired = recipesByItemId
            .getOrDefault(parentOfParentItemId, Map.of())
            .getOrDefault(itemIdToCraft, 0L);

        Long multipliedQuantity = baseQuantityRequired * calculateParentMultiplier(parentIngredient, ingredientsById, recipesByItemId, mainItem);

        logger.debug("Ligne #134 || multipliedQuantity : {}", multipliedQuantity);

        workshopRepository.updateIngredientQuantityObtained(userId, ingredientId, multipliedQuantity);

        return enrichCreatedIngredients(userId, ingredientId, gameVersionId);
    }

    private List<WorkshopIngredientDetailDto> enrichCreatedIngredients(
        Long userId,
        Long parentIngredientId,
        Long gameVersionId
    ) {
        GameVersionData gameVersion = gameVersionRepository.findById(gameVersionId)
            .orElseThrow(() -> new IllegalArgumentException("Game version not found for id: " + gameVersionId));

        List<WorkshopItemIngredient> ingredients = workshopRepository.findIngredientsByParentIngredientId(userId, parentIngredientId);

        Map<Long, WorkshopItemIngredient> ingredientsById = new HashMap<>();
        Set<Long> allItemIds = new HashSet<>();

        for (WorkshopItemIngredient ing : ingredients) {
            allItemIds.add(ing.getItemId());
            ingredientsById.put(ing.getId(), ing);
        }

        WorkshopItemIngredient parentIngredient = workshopRepository.findIngredientByIdAndUserId(userId, parentIngredientId).orElseThrow();
        allItemIds.add(parentIngredient.getItemId());

        Map<Long, Map<Long, Long>> recipesByItemId = new HashMap<>();
        for (Long itemId : allItemIds) {
            List<Recipe> recipes = recipeRepository.findByItemId(itemId);
            Map<Long, Long> ingredientQuantities = new HashMap<>();
            for (Recipe recipe : recipes) {
                ingredientQuantities.put(recipe.getIngredientId(), recipe.getQuantity());
            }
            recipesByItemId.put(itemId, ingredientQuantities);
        }

        Map<Long, ItemDto> itemsById = itemRepository.findByGameVersionIdAndItemIds(gameVersionId, allItemIds);
        Map<Long, List<FarmZoneDto>> farmZonesByItemId = itemEnrichmentService.loadFarmZones(new ArrayList<>(allItemIds), gameVersion.getCode());
        Map<Long, List<ItemImageDto>> imagesByItemId = itemEnrichmentService.loadItemImages(new ArrayList<>(allItemIds), gameVersion.getCode());

        List<WorkshopIngredientDetailDto> result = new ArrayList<>();

        for (WorkshopItemIngredient ingredient : ingredients) {
            Long quantityRequired = recipesByItemId
                .getOrDefault(parentIngredient.getItemId(), Map.of())
                .getOrDefault(ingredient.getItemId(), 0L);

            ItemDto ingredientItemDto = itemsById.get(ingredient.getItemId());
            List<ItemImageDto> imagesIngredientDto = imagesByItemId.getOrDefault(ingredient.getItemId(), List.of());
            List<FarmZoneDto> farmZonesIngredientDto = farmZonesByItemId.getOrDefault(ingredient.getItemId(), List.of());

            ItemDto enrichedIngredientItemDto = new ItemDto(
                ingredientItemDto.getId(),
                ingredientItemDto.getName(),
                ingredientItemDto.getDescription(),
                ingredientItemDto.isHasRecipe(),
                ingredientItemDto.getAssetId(),
                ingredientItemDto.getGameVersionId(),
                ingredientItemDto.getLevel(),
                ingredientItemDto.getType(),
                imagesIngredientDto,
                ingredientItemDto.getParentItemId(),
                quantityRequired,
                farmZonesIngredientDto
            );

            result.add(
                new WorkshopIngredientDetailDto(
                    ingredient.getId(),
                    ingredient.getWorkshopItemId(),
                    enrichedIngredientItemDto,
                    ingredient.getParentIngredientId(),
                    ingredient.getQuantityObtained(),
                    quantityRequired
                )
            );
        }

        return result;
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