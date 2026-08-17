package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncData;

public interface ElementDataProvider {
    List<ElementSyncData> fetchAll();
}
