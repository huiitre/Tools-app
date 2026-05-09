package fr.huiitre.tools.modules.dofus.catalogue.application.usecase;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.catalogue.application.ports.CatalogueItemRepository;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.dto.FarmZoneDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.application.service.ItemEnrichmentService;
import fr.huiitre.tools.modules.dofus.sync.application.views.AssetImageUrlBuilder;

@Service
@Transactional(readOnly = true)
public class GetCatalogueRecipeItemUseCase implements SecuredUseCase {

    private final CatalogueItemRepository catalogueItemRepository;
    private final ItemRepository itemRepository;
    private final AssetImageUrlBuilder assetImageUrlBuilder;
    private final ItemEnrichmentService itemEnrichmentService;
    private final GameVersionRepository gameVersionRepository;

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public GetCatalogueRecipeItemUseCase(
            CatalogueItemRepository catalogueItemRepository,
            ItemRepository itemRepository,
            AssetImageUrlBuilder assetImageUrlBuilder,
            ItemEnrichmentService itemEnrichmentService,
            GameVersionRepository gameVersionRepository) {
        this.catalogueItemRepository = catalogueItemRepository;
        this.itemRepository = itemRepository;
        this.assetImageUrlBuilder = assetImageUrlBuilder;
        this.itemEnrichmentService = itemEnrichmentService;
        this.gameVersionRepository = gameVersionRepository;
    }

    public List<ItemDto> execute(Long itemId, Long gameServerId) {

        GameVersionData gameVersionData = gameVersionRepository.findByGameServerId(gameServerId)
                .orElseThrow(() -> new IllegalArgumentException("Game version not found for gameServerId: " + gameServerId));

        List<ItemDto> ingredients = catalogueItemRepository.findRecipeByItemId(itemId);

        List<Long> itemIds = ingredients.stream()
                .map(ItemDto::getId)
                .collect(Collectors.toList());

        Map<Long, List<FarmZoneDto>> farmZonesByItemId = itemEnrichmentService.loadFarmZones(new ArrayList<>(itemIds), gameVersionData.getCode());
        Map<Long, List<ItemImageDto>> imagesByItemId = itemEnrichmentService.loadItemImages(new ArrayList<>(itemIds), gameVersionData.getCode());

        for (int i = 0; i < ingredients.size(); i++) {

            ItemDto ingredient = ingredients.get(i);

            List<FarmZoneDto> farmZones = farmZonesByItemId.get(ingredient.getId());
            List<ItemImageDto> images = imagesByItemId.get(ingredient.getId());

            ItemDto itemWithImages = new ItemDto(
                    ingredient.getId(),
                    ingredient.getName(),
                    ingredient.getDescription(),
                    ingredient.isHasRecipe(),
                    ingredient.getAssetId(),
                    ingredient.getGameVersionId(),
                    ingredient.getLevel(),
                    ingredient.getType(),
                    images,
                    ingredient.getParentItemId(),
                    ingredient.getQuantity(),
                    farmZones);

            ingredients.set(i, itemWithImages);
        }

        return ingredients;
    }
}