package fr.huiitre.tools.modules.core.module.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.application.command.CreateModuleCommand;
import fr.huiitre.tools.modules.core.module.application.ports.ModuleRepository;
import fr.huiitre.tools.modules.core.module.domain.Module;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class CreateModuleUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final ModuleRepository moduleRepository;

    public CreateModuleUseCase(
            ModuleRepository moduleRepository) {
        this.moduleRepository = moduleRepository;
    }

    public Module execute(CreateModuleCommand command) {

        System.out.println("CreateModuleUseCase.execute : " + command.getCode());

        // * vérification si le module existe déjà (code) */
        if (moduleRepository.existsByCode(command.getCode()))
            throw new IllegalArgumentException("MODULE_ALREADY_EXISTS");

        // * le module n'existe pas, on peut le créer */
        Module module = Module.create(
                command.getCode(),
                command.getName(),
                command.getDescription());

        moduleRepository.save(module);

        return module;
    }
}
