package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.WorkPrioritySyncData;

public interface WorkPriorityDataProvider {
    List<WorkPrioritySyncData> fetchAll();
}
