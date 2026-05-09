package fr.huiitre.tools.modules.riot.sync.application;

import fr.huiitre.tools.modules.riot.valorant.application.view.ValorantSkinView;

import java.util.List;

public interface ValorantSkinSyncRepository {
    List<ValorantSkinView> findAll();
    Long save(ValorantSkinSyncData data, Long weaponId);
    void update(Long id, ValorantSkinSyncData data, Long weaponId);
    void delete(Long id);
}
