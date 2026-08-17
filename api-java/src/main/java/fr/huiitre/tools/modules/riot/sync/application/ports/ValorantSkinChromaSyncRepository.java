package fr.huiitre.tools.modules.riot.sync.application.ports;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinChromaSyncData;

public interface ValorantSkinChromaSyncRepository {
    void deleteAll();
    void save(Long skinId, ValorantSkinChromaSyncData data);
}
