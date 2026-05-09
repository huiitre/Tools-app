package fr.huiitre.tools.modules.dofus.catalogue.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.catalogue.api.dto.CatalogueSearchQuery;
import fr.huiitre.tools.modules.dofus.item.application.dto.ItemDto;

public interface CatalogueItemRepository {

    public List<ItemDto> search(
            CatalogueSearchQuery query,
            Long userId,
            Long gameVersionId);

    public Long count(
            CatalogueSearchQuery query,
            Long userId,
            Long gameVersionId);

    public List<ItemDto> findRecipeByItemId(Long itemId);
}
