package fr.huiitre.tools.modules.palworld.sync.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncData;
import fr.huiitre.tools.modules.palworld.sync.application.view.WorkSuitabilityRefView;

public interface WorkSuitabilitySyncRepository {
    List<WorkSuitabilityRefView> findAll();
    Long save(WorkSuitabilitySyncData data);
    void update(Long id, WorkSuitabilitySyncData data);
    void delete(Long id);
}
