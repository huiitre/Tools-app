package fr.huiitre.tools.modules.riot.sync.application.ports;

import fr.huiitre.tools.modules.riot.sync.application.ValorantSkinSyncData;
import fr.huiitre.tools.modules.riot.valorant.application.skin.view.ValorantSkinView;

import java.util.List;

public interface ValorantSkinSyncRepository {
    List<ValorantSkinView> findAll();
    Long save(ValorantSkinSyncData data, Long weaponId);
    void update(Long id, ValorantSkinSyncData data, Long weaponId);
    void delete(Long id);
}
