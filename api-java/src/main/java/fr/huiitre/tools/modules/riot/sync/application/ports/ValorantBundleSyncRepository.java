package fr.huiitre.tools.modules.riot.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.riot.sync.application.ValorantBundleSyncData;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantBundleView;

public interface ValorantBundleSyncRepository {
    List<ValorantBundleView> findAll();
    Long save(ValorantBundleSyncData data);
    void update(Long id, ValorantBundleSyncData data);
    void delete(Long id);
}
