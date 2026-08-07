package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.ItemSyncData;

public interface ItemDataProvider {
    List<ItemSyncData> fetchAll();
}
