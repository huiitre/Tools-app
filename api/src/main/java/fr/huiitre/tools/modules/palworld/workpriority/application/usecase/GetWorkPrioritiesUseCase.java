package fr.huiitre.tools.modules.palworld.workpriority.application.usecase;

import java.util.List;
import java.util.Optional;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.workpriority.application.ports.WorkPriorityRepository;
import fr.huiitre.tools.modules.palworld.workpriority.application.view.WorkPriorityView;

@Service
public class GetWorkPrioritiesUseCase implements SecuredUseCase {

    private final WorkPriorityRepository workPriorityRepository;

    public GetWorkPrioritiesUseCase(WorkPriorityRepository workPriorityRepository) {
        this.workPriorityRepository = workPriorityRepository;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<WorkPriorityView> execute() {
        return workPriorityRepository.findAll();
    }
}
