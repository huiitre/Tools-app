package fr.huiitre.tools.modules.palworld.tierlist.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.tierlist.application.TierListSourceSyncData;

public interface TierListDataProvider {
    List<TierListSourceSyncData> fetchAll();
}
