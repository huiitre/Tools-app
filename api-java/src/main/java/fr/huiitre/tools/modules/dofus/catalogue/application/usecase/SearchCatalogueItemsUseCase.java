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
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.catalogue.api.dto.CatalogueSearchQuery;
import fr.huiitre.tools.modules.dofus.catalogue.application.data.CatalogueColumnsDefinition;
import fr.huiitre.tools.modules.dofus.catalogue.application.dto.CatalogueSearchResponse;
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
public class SearchCatalogueItemsUseCase implements SecuredUseCase {

    private static final int DEFAULT_PAGE = 1;
    private static final int DEFAULT_PAGE_SIZE = 20;

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final CatalogueItemRepository catalogueItemRepository;
    private final ItemRepository itemRepository;
    private final ItemEnrichmentService itemEnrichmentService;
    private final AssetImageUrlBuilder assetImageUrlBuilder;
    private final GameVersionRepository gameVersionRepository;

    public SearchCatalogueItemsUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            CatalogueItemRepository catalogueItemRepository,
            ItemRepository itemRepository,
            AssetImageUrlBuilder assetImageUrlBuilder,
            ItemEnrichmentService itemEnrichmentService,
            GameVersionRepository gameVersionRepository) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.catalogueItemRepository = catalogueItemRepository;
        this.itemRepository = itemRepository;
        this.assetImageUrlBuilder = assetImageUrlBuilder;
        this.itemEnrichmentService = itemEnrichmentService;
        this.gameVersionRepository = gameVersionRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public CatalogueSearchResponse execute(
            CatalogueSearchQuery query,
            Long gameServerId) {
        Long userId = authenticatedUserProvider.getUserId();

        GameVersionData gameVersion = gameVersionRepository.findByGameServerId(gameServerId)
            .orElseThrow(() -> new IllegalArgumentException("Serveur invalide ou version introuvable"));

        int page = query.getPage() == null || query.getPage() < 1
            ? DEFAULT_PAGE
            : query.getPage();

        int pageSize = query.getPageSize() == null || query.getPageSize() < 1
            ? DEFAULT_PAGE_SIZE
            : query.getPageSize();

        query.setPage(page);
        query.setPageSize(pageSize);

        List<ItemDto> items = catalogueItemRepository.search(
            query,
            userId,
            gameVersion.getId());

        List<Long> itemIds = items.stream()
            .map(ItemDto::getId)
            .collect(Collectors.toList());

        Map<Long, List<FarmZoneDto>> farmZonesByItemId = itemEnrichmentService.loadFarmZones(new ArrayList<>(itemIds), gameVersion.getCode());
        Map<Long, List<ItemImageDto>> imagesByItemId = itemEnrichmentService.loadItemImages(new ArrayList<>(itemIds), gameVersion.getCode());

        for (ItemDto item : items) {

            List<ItemImageDto> itemImages = imagesByItemId.get(item.getId());

            List<FarmZoneDto> farmZones = farmZonesByItemId.get(item.getId());

            ItemDto itemWithImages = new ItemDto(
                item.getId(),
                item.getName(),
                item.getDescription(),
                item.isHasRecipe(),
                item.getAssetId(),
                item.getGameVersionId(),
                item.getLevel(),
                item.getType(),
                itemImages,
                item.getParentItemId(),
                item.getQuantity(),
                farmZones);

            items.set(items.indexOf(item), itemWithImages);
        }


        long total = catalogueItemRepository.count(
                query,
                userId,
                gameVersion.getId());

        int computedLastPage = (int) Math.max(
                1,
                Math.ceil((double) total / pageSize));

        Integer previousPage = page > 1 ? page - 1 : null;

        Integer nextPage = page < computedLastPage ? page + 1 : null;

        Integer lastPage = page < computedLastPage ? computedLastPage : null;

        return new CatalogueSearchResponse(
                CatalogueColumnsDefinition.all(),
                items,
                page,
                pageSize,
                total,
                previousPage,
                nextPage,
                lastPage);
    }
}
