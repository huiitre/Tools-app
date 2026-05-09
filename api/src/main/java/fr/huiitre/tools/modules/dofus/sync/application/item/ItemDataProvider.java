package fr.huiitre.tools.modules.dofus.sync.application.item;

import java.util.List;

public interface ItemDataProvider {
    boolean supports(String gameVersionCode);
    List<ItemSyncData> fetchAll();
}