package fr.huiitre.tools.modules.dofus.sync.application.itemtype;

import java.util.List;

public interface ItemTypeDataProvider {
    boolean supports(String gameVersionCode);
    List<ItemTypeSyncData> fetchAll();
}