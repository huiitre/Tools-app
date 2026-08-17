package fr.huiitre.tools.modules.dofus.pricing.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.pricing.application.view.ItemPriceDto;

public interface ItemPriceRepository {

    List<ItemPriceDto> findPricesByItemIds(List<Long> itemIds, Long userId, Long serverId);

    void updateItemPrice(Long itemId, Long serverId, Long userId, Long price);
}
