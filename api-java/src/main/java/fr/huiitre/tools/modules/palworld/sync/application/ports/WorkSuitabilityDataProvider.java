package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncData;

public interface WorkSuitabilityDataProvider {
    List<WorkSuitabilitySyncData> fetchAll();
}
