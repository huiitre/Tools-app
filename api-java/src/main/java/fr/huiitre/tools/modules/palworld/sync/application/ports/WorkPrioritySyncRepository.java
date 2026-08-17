package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.WorkPrioritySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.view.WorkPriorityRefView;

public interface WorkPrioritySyncRepository {
    List<WorkPriorityRefView> findAll();
    Long save(WorkPrioritySyncData data, Long workSuitabilityId);
    void update(Long id, WorkPrioritySyncData data, Long workSuitabilityId);
    void delete(Long id);
}
