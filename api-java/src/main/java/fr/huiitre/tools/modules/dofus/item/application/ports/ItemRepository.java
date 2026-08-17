package fr.huiitre.tools.modules.dofus.item.application.ports;

import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.item.application.dto.FarmZoneDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemImageDto;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemLightDTO;
import fr.huiitre.tools.modules.dofus.item.domain.Item;

public interface ItemRepository {

    List<Item> findAllByGameVersionId(Long gameVersionId);

    Long save(Item item);

    void update(Item item);

    boolean refreshImages(Long itemId, Long iconId, GameVersionData gameVersionData);

    Optional<Item> findByAssetId(Long assetId, long gameVersionId);

    List<ItemImageDto> findImageByItemId(Long itemId);

    List<ItemImageDto> findImageByItemIds(Collection<Long> itemIds);

    Map<Long, ItemDto> findByGameVersionIdAndItemIds(Long gameVersionId, Collection<Long> itemIds);

    List<ItemLightDTO> findLightByAssetIds(Long gameVersionId, Collection<Long> assetIds);

    Map<Long, List<FarmZoneDto>> findFarmZonesByItemIds(Collection<Long> itemIds);

    List<ItemDto> findCraftableItemsByGameVersionIdAndName(Long gameVersionId, Long workshopId, String query);
}
