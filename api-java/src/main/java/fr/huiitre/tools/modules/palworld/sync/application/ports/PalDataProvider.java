package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.PalSyncData;

public interface PalDataProvider {
    List<PalSyncData> fetchAll();
}
