package fr.huiitre.tools.modules.riot.sync.application.ports;

import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponSyncData;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.view.ValorantWeaponView;

import java.util.List;

public interface ValorantWeaponSyncRepository {
    List<ValorantWeaponView> findAll();
    Long save(ValorantWeaponSyncData data);
    void update(Long id, ValorantWeaponSyncData data);
    void delete(Long id);
}
