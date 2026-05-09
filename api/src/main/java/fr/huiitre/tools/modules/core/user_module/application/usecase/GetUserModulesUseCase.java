package fr.huiitre.tools.modules.core.user_module.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.user_module.application.view.UserModuleView;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

@Service
@Transactional
public class GetUserModulesUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final UserModuleRoleRepository userModuleRoleRepository;

    public GetUserModulesUseCase(
            UserModuleRoleRepository userModuleRoleRepository) {
        this.userModuleRoleRepository = userModuleRoleRepository;
    }

    public List<UserModuleView> execute(Long userId) {
        return userModuleRoleRepository.findAllByUserId(userId);
    }
}