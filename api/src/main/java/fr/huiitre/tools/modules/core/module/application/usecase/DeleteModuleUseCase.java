package fr.huiitre.tools.modules.core.module.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.module.application.ports.ModuleRepository;
import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.module.domain.Module;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class DeleteModuleUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final ModuleRepository moduleRepository;
    private final UserModuleRoleRepository userModuleRoleRepository;

    public DeleteModuleUseCase(
            ModuleRepository moduleRepository,
            UserModuleRoleRepository userModuleRoleRepository) {
        this.moduleRepository = moduleRepository;
        this.userModuleRoleRepository = userModuleRoleRepository;
    }

    public void execute(Long moduleId) {

        // * vérification si le module existe */
        Module module = moduleRepository.findById(moduleId)
                .orElseThrow(() -> new IllegalArgumentException("MODULE_NOT_FOUND"));

        // * suppression des utilisateurs utilisant ce module */
        userModuleRoleRepository.deleteByModuleId(moduleId);

        // * suppression du module */
        moduleRepository.delete(module);
    }
}
