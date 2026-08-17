package fr.huiitre.tools.modules.riot.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.riot.sync.application.ValorantContentTierSyncData;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantContentTierView;

public interface ValorantContentTierSyncRepository {
    List<ValorantContentTierView> findAll();
    Long save(ValorantContentTierSyncData data);
    void update(Long id, ValorantContentTierSyncData data);
    void delete(Long id);
}