package fr.huiitre.tools.modules.palworld.workpriority.api;

import java.util.List;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.common.api.RequiredRole;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.palworld.workpriority.application.usecase.GetWorkPrioritiesUseCase;
import fr.huiitre.tools.modules.palworld.workpriority.application.view.WorkPriorityView;

@RestController
@RequestMapping("/palworld/work-priorities")
public class PalworldWorkPriorityController {

    private final GetWorkPrioritiesUseCase getWorkPrioritiesUseCase;

    public PalworldWorkPriorityController(GetWorkPrioritiesUseCase getWorkPrioritiesUseCase) {
        this.getWorkPrioritiesUseCase = getWorkPrioritiesUseCase;
    }

    @RequiredRole(RoleCode.READ_ONLY)
    @GetMapping
    public List<WorkPriorityView> getWorkPriorities() {
        return getWorkPrioritiesUseCase.execute();
    }
}
