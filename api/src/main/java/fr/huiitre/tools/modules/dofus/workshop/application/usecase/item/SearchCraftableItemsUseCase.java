package fr.huiitre.tools.modules.dofus.workshop.application.usecase.item;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

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

@Service
@Transactional
public class SearchCraftableItemsUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

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

    public SearchCraftableItemsUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            ItemRepository itemRepository,
            ItemEnrichmentService itemEnrichmentService,
            GameVersionRepository gameVersionRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.itemRepository = itemRepository;
        this.itemEnrichmentService = itemEnrichmentService;
        this.gameVersionRepository = gameVersionRepository;
    }

    public List<ItemDto> execute(Long gameVersionId, Long workshopId, String query) {

        GameVersionData gameVersion = gameVersionRepository.findById(gameVersionId)
            .orElseThrow(() -> new IllegalArgumentException("Game version not found for id: " + gameVersionId));

        List<ItemDto> items = itemRepository.findCraftableItemsByGameVersionIdAndName(gameVersionId, workshopId, query);

        List<ItemDto> enrichedItems = new ArrayList<>();

        List<Long> itemIds = items.stream()
                .map(ItemDto::getId)
                .toList();

        Map<Long, List<ItemImageDto>> itemImages = itemEnrichmentService.loadItemImages(itemIds, gameVersion.getCode());

        Map<Long, List<FarmZoneDto>> farmZones = itemEnrichmentService.loadFarmZones(itemIds, gameVersion.getCode());

        for (ItemDto item : items) {
            enrichedItems.add(
                    new ItemDto(
                            item.getId(),
                            item.getName(),
                            item.getDescription(),
                            item.isHasRecipe(),
                            item.getAssetId(),
                            item.getGameVersionId(),
                            item.getLevel(),
                            item.getType(),
                            itemImages.getOrDefault(item.getId(), List.of()),
                            item.getParentItemId(),
                            item.getQuantity(),
                            farmZones.getOrDefault(item.getId(), List.of())));
        }

        return enrichedItems;
    }
}