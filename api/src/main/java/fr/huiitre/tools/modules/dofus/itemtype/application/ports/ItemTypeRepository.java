package fr.huiitre.tools.modules.dofus.itemtype.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.dofus.itemtype.domain.ItemType;

public interface ItemTypeRepository {

    List<ItemType> findAllByGameVersionId(Long gameVersionId);

    void save(ItemType itemType);

    void update(ItemType itemType);

}
