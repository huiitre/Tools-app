package fr.huiitre.tools.modules.riot.sync.application.ports;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinLevelSyncData;

public interface ValorantSkinLevelSyncRepository {
    void deleteAll();
    void save(Long skinId, ValorantSkinLevelSyncData data);
}
