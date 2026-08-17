package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.BreedingExceptionSyncData;

public interface BreedingExceptionDataProvider {
    List<BreedingExceptionSyncData> fetchAll();
}
