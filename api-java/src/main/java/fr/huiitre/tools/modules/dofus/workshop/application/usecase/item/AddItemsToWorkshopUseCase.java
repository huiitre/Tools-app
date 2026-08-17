package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

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
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopItemDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItem;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

@Service
@Transactional
public class AddItemsToWorkshopUseCase implements SecuredUseCase {

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

    public AddItemsToWorkshopUseCase(
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

    public List<WorkshopItemDetailDto> execute(Long workshopId, Long gameVersionId, List<Long> itemIds) {
    
        Long userId = authenticatedUserProvider.getUserId();
        
        boolean exists = workshopRepository.existsByIdAndUserId(userId, workshopId);
        if (!exists) {
            throw new WorkshopNotFoundException();
        }

        List<WorkshopItem> existingItems = workshopRepository.findAllItemsByUserIdAndWorkshopId(userId, workshopId);
        Set<Long> existingItemIds = existingItems.stream()
            .map(WorkshopItem::getItemId)
            .collect(Collectors.toSet());

        List<Long> createdWorkshopItemIds = new ArrayList<>();

        for (Long itemId : itemIds) {
            if (existingItemIds.contains(itemId)) {
                continue;
            }
            
            WorkshopItem item = WorkshopItem.create(itemId, 1L);
            Long workshopItemId = workshopRepository.addItemToWorkshop(userId, workshopId, item);
            createdWorkshopItemIds.add(workshopItemId);
            
            // Ajouter uniquement les ingrédients directs
            List<Recipe> recipes = recipeRepository.findByItemId(itemId);
            List<WorkshopItemIngredient> ingredients = new ArrayList<>();
            
            for (Recipe recipe : recipes) {
                WorkshopItemIngredient ingredient = WorkshopItemIngredient.create(
                    workshopItemId,
                    recipe.getIngredientId(),
                    null,  // parent_ingredient_id = NULL (ingrédients directs)
                    0L
                );
                ingredients.add(ingredient);
            }
            
            workshopRepository.addIngredients(userId, ingredients);
        }

        return enrichCreatedItems(userId, workshopId, createdWorkshopItemIds, gameVersionId);
    }

    private List<WorkshopItemDetailDto> enrichCreatedItems(
        Long userId,
        Long workshopId,
        List<Long> workshopItemIds,
        Long gameVersionId
    ) {

        GameVersionData gameVersionData = gameVersionRepository.findById(gameVersionId)
            .orElseThrow(() -> new IllegalArgumentException("Game version not found for id: " + gameVersionId));

        List<WorkshopItem> items = workshopRepository.findAllItemsByUserIdAndWorkshopId(userId, workshopId)
            .stream()
            .filter(item -> workshopItemIds.contains(item.getId()))
            .toList();

        Map<Long, List<WorkshopItemIngredient>> ingredientsByWorkshopItemId = new HashMap<>();
        Map<Long, WorkshopItemIngredient> ingredientsById = new HashMap<>();
        Set<Long> allItemIds = new HashSet<>();

        for (WorkshopItem item : items) {
            allItemIds.add(item.getItemId());
            List<WorkshopItemIngredient> ingredients = workshopRepository.findAllIngredientsByUserIdAndWorkshopItemId(userId, item.getId());
            ingredientsByWorkshopItemId.put(item.getId(), ingredients);
            
            for (WorkshopItemIngredient ing : ingredients) {
                allItemIds.add(ing.getItemId());
                ingredientsById.put(ing.getId(), ing);
            }
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

        Map<Long, ItemDto> itemsById = itemRepository.findByGameVersionIdAndItemIds(gameVersionId, allItemIds);
        Map<Long, List<FarmZoneDto>> farmZonesByItemId = itemEnrichmentService.loadFarmZones(new ArrayList<>(allItemIds), gameVersionData.getCode());
        Map<Long, List<ItemImageDto>> imagesByItemId = itemEnrichmentService.loadItemImages(new ArrayList<>(allItemIds), gameVersionData.getCode());

        List<WorkshopItemDetailDto> result = new ArrayList<>();

        for (WorkshopItem item : items) {
            ItemDto itemDto = itemsById.get(item.getItemId());
            List<ItemImageDto> imagesDto = imagesByItemId.getOrDefault(item.getItemId(), List.of());
            List<FarmZoneDto> farmZonesDto = farmZonesByItemId.getOrDefault(item.getItemId(), List.of());

            ItemDto enrichedItemDto = new ItemDto(
                itemDto.getId(),
                itemDto.getName(),
                itemDto.getDescription(),
                itemDto.isHasRecipe(),
                itemDto.getAssetId(),
                itemDto.getGameVersionId(),
                itemDto.getLevel(),
                itemDto.getType(),
                imagesDto,
                null,
                item.getQuantity(),
                farmZonesDto
            );

            List<WorkshopItemIngredient> ingredients = ingredientsByWorkshopItemId.get(item.getId());
            List<WorkshopIngredientDetailDto> ingredientList = new ArrayList<>();

            for (WorkshopItemIngredient ingredient : ingredients) {
                Long parentItemId;
                if (ingredient.getParentIngredientId() == null) {
                    parentItemId = item.getItemId();
                } else {
                    WorkshopItemIngredient parentIngredient = ingredientsById.get(ingredient.getParentIngredientId());
                    parentItemId = parentIngredient.getItemId();
                }
                
                Long quantityRequired = recipesByItemId
                    .getOrDefault(parentItemId, Map.of())
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

                ingredientList.add(
                    new WorkshopIngredientDetailDto(
                        ingredient.getId(),
                        item.getId(),
                        enrichedIngredientItemDto,
                        ingredient.getParentIngredientId(),
                        ingredient.getQuantityObtained(),
                        quantityRequired
                    )
                );
            }

            result.add(new WorkshopItemDetailDto(
                item.getId(),
                workshopId,
                enrichedItemDto,
                item.getQuantity(),
                ingredientList
            ));
        }

        return result;
    }
}