package fr.huiitre.tools.modules.core.module.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.application.ports.ModuleRepository;
import fr.huiitre.tools.modules.core.module.domain.Module;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;

@Service
@Transactional
public class GetAllModulesUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final ModuleRepository moduleRepository;

    public GetAllModulesUseCase(
            ModuleRepository moduleRepository) {
        this.moduleRepository = moduleRepository;
    }

    public List<Module> execute() {
        return moduleRepository.findAll();
    }
}