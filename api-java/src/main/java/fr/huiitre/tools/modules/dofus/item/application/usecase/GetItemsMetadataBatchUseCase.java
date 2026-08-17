package fr.huiitre.tools.modules.dofus.item.application.usecase;

import java.util.List;
import java.util.Map;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemLightDTO;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.application.service.ItemEnrichmentService;

@Service
@Transactional(readOnly = true)
public class GetItemsMetadataBatchUseCase implements SecuredUseCase {

    private final static Logger logger = LoggerFactory.getLogger(GetItemsMetadataBatchUseCase.class);

    private final ItemRepository itemRepository;
    private final ItemEnrichmentService itemEnrichmentService;
    private final GameVersionRepository gameVersionRepository;

    public GetItemsMetadataBatchUseCase(
            ItemRepository itemRepository,
            ItemEnrichmentService itemEnrichmentService,
            GameVersionRepository gameVersionRepository) {
        this.itemRepository = itemRepository;
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

    public List<ItemLightDTO> execute(Long gameVersionId, List<Long> assetIds) {

        logger.debug("Ligne #49 || gameVersionId : {}", gameVersionId);

        List<ItemLightDTO> items = this.itemRepository.findLightByAssetIds(gameVersionId, assetIds);

        if (items.isEmpty()) {
            return items;
        }

        GameVersionData gameVersionData = gameVersionRepository.findById(gameVersionId)
                .orElseThrow(() -> new IllegalArgumentException("GAME_VERSION_NOT_FOUND"));

        List<Long> itemIds = items.stream()
                .map(ItemLightDTO::getId)
                .toList();

        Map<Long, List<ItemImageDto>> imagesByItemId = itemEnrichmentService.loadItemImages(itemIds, gameVersionData.getCode());

        return items.stream()
                .map(item -> new ItemLightDTO(
                        item.getId(),
                        item.getName(),
                        item.getLevel(),
                        item.getGameVersionId(),
                        item.getAssetId(),
                        item.getItemType(),
                        imagesByItemId.getOrDefault(item.getId(), List.of())))
                .toList();
    }
}
