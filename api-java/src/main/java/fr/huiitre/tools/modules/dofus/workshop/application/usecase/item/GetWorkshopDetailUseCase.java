package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

import java.util.ArrayList;
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
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.dto.FarmZoneDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.application.service.ItemEnrichmentService;
import fr.huiitre.tools.modules.dofus.recipe.application.ports.RecipeRepository;
import fr.huiitre.tools.modules.dofus.recipe.domain.Recipe;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopDetailResponse;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopIngredientDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopItemDetailDto;
import fr.huiitre.tools.modules.dofus.workshop.application.dto.WorkshopLinkDto;
import fr.huiitre.tools.modules.dofus.workshop.application.exception.WorkshopNotFoundException;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.Workshop;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItem;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopItemIngredient;

@Service
@Transactional
public class GetWorkshopDetailUseCase implements SecuredUseCase {

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

    public GetWorkshopDetailUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            WorkshopRepository workshopRepository,
            RecipeRepository recipeRepository,
            ItemRepository itemRepository,
            ItemEnrichmentService itemEnrichmentService,
            GameVersionRepository gameVersionRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.workshopRepository = workshopRepository;
        this.recipeRepository = recipeRepository;
        this.itemRepository = itemRepository;
        this.itemEnrichmentService = itemEnrichmentService;
        this.gameVersionRepository = gameVersionRepository;
    }

    public WorkshopDetailResponse execute(Long workshopId, Long gameVersionId) {

        GameVersionData gameVersion = gameVersionRepository.findById(gameVersionId)
            .orElseThrow(() -> new IllegalArgumentException("Game version not found for id: " + gameVersionId));

        Long userId = authenticatedUserProvider.getUserId();

        Workshop workshopDomain = workshopRepository.findById(workshopId, gameVersion.getId())
            .orElseThrow(WorkshopNotFoundException::new);

        Long ownerUserId = workshopRepository.findOwnerUserId(workshopId);

        boolean isOwner = ownerUserId.equals(userId);

        Long effectiveUserId = isOwner ? userId : ownerUserId;

        List<WorkshopItem> items = workshopRepository.findAllItemsByUserIdAndWorkshopId(effectiveUserId, workshopId);

        // Récupérer tous les ingrédients + Map par ID
        Map<Long, List<WorkshopItemIngredient>> ingredientsByWorkshopItemId = new HashMap<>();
        Map<Long, WorkshopItemIngredient> ingredientsById = new HashMap<>();
        Set<Long> allItemIds = new HashSet<>();

        for (WorkshopItem item : items) {
            allItemIds.add(item.getItemId());
            List<WorkshopItemIngredient> ingredients = workshopRepository
                    .findAllIngredientsByUserIdAndWorkshopItemId(effectiveUserId, item.getId());
            ingredientsByWorkshopItemId.put(item.getId(), ingredients);

            for (WorkshopItemIngredient ing : ingredients) {
                allItemIds.add(ing.getItemId());
                ingredientsById.put(ing.getId(), ing);
            }
        }

        // Charger toutes les recettes
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
        Map<Long, List<FarmZoneDto>> farmZonesByItemId = itemEnrichmentService
                .loadFarmZones(new ArrayList<>(allItemIds), gameVersion.getCode());
        Map<Long, List<ItemImageDto>> imagesByItemId = itemEnrichmentService
                .loadItemImages(new ArrayList<>(allItemIds), gameVersion.getCode());

        List<WorkshopItemDetailDto> itemList = new ArrayList<>();

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
                    farmZonesDto);

            List<WorkshopItemIngredient> ingredients = ingredientsByWorkshopItemId.get(item.getId());
            List<WorkshopIngredientDetailDto> ingredientList = new ArrayList<>();

            for (WorkshopItemIngredient ingredient : ingredients) {
                // Déterminer l'item parent pour récupérer quantity_required
                Long parentItemId;
                if (ingredient.getParentIngredientId() == null) {
                    parentItemId = item.getItemId();
                } else {
                    WorkshopItemIngredient parentIngredient = ingredientsById.get(ingredient.getParentIngredientId());
                    parentItemId = parentIngredient.getItemId();
                }
                
                Long baseQuantityRequired = recipesByItemId
                    .getOrDefault(parentItemId, Map.of())
                    .getOrDefault(ingredient.getItemId(), 0L);

                Long quantityRequired;
                //* Ingrédient direct de l'item principal
                if (ingredient.getParentIngredientId() == null) {
                    quantityRequired = baseQuantityRequired * item.getQuantity();
                }
                //* Sous-ingrédient d'un ingrédient crafté
                else {    
                    quantityRequired = baseQuantityRequired * calculateParentMultiplier(ingredient, ingredientsById, recipesByItemId, item);
                }


                ItemDto ingredientItemDto = itemsById.get(ingredient.getItemId());
                List<ItemImageDto> imagesIngredientDto = imagesByItemId.getOrDefault(ingredient.getItemId(), List.of());
                List<FarmZoneDto> farmZonesIngredientDto = farmZonesByItemId.getOrDefault(ingredient.getItemId(),
                        List.of());

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
                        baseQuantityRequired,
                        farmZonesIngredientDto);

                ingredientList.add(
                        new WorkshopIngredientDetailDto(
                                ingredient.getId(),
                                item.getId(),
                                enrichedIngredientItemDto,
                                ingredient.getParentIngredientId(),
                                ingredient.getQuantityObtained(),
                                quantityRequired));
            }

            WorkshopItemDetailDto itemDetail = new WorkshopItemDetailDto(
                    item.getId(),
                    workshopId,
                    enrichedItemDto,
                    item.getQuantity(),
                    ingredientList);

            itemList.add(itemDetail);
        }

        List<WorkshopLinkDto> linksDto = workshopDomain.getLinks().stream()
            .map(link -> new WorkshopLinkDto(link.getId(), link.getSource(), link.getUrl(), link.getLabel()))
            .toList();

        return new WorkshopDetailResponse(itemList, linksDto, isOwner);
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