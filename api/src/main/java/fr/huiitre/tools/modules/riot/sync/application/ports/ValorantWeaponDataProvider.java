package fr.huiitre.tools.modules.riot.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.riot.sync.application.ValorantWeaponSyncData;

public interface ValorantWeaponDataProvider {
    List<ValorantWeaponSyncData> fetchAll();
}
