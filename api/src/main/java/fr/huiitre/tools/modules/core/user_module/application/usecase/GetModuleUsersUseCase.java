package fr.huiitre.tools.modules.core.user_module.application.usecase;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.user_module.application.view.ModuleUserView;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@Transactional
public class GetModuleUsersUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final UserModuleRoleRepository userModuleRoleRepository;

    public GetModuleUsersUseCase(UserModuleRoleRepository userModuleRoleRepository) {
        this.userModuleRoleRepository = userModuleRoleRepository;
    }

    public List<ModuleUserView> execute(Long moduleId) {
        return userModuleRoleRepository.findAllByModuleId(moduleId);
    }
}
