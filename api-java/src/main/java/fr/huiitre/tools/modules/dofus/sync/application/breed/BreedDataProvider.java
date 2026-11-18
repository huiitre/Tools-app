package fr.huiitre.tools.modules.dofus.sync.application.breed;

import java.util.List;

public interface BreedDataProvider {

    List<BreedSyncData> fetchAll();
}
