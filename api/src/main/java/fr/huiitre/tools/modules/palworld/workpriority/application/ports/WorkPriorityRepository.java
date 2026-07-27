package fr.huiitre.tools.modules.palworld.workpriority.application.ports;

import java.util.List;

import fr.huiitre.tools.modules.palworld.workpriority.application.view.WorkPriorityView;

public interface WorkPriorityRepository {
    List<WorkPriorityView> findAll();
}
