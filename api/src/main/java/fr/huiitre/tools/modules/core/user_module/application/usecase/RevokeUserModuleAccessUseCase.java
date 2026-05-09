package fr.huiitre.tools.modules.core.user_module.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.user_module.application.command.RevokeUserModuleAccessCommand;
import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.user_module.domain.UserModuleRole;

@Service
@Transactional
public class RevokeUserModuleAccessUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final UserModuleRoleRepository userModuleRoleRepository;

    public RevokeUserModuleAccessUseCase(UserModuleRoleRepository userModuleRoleRepository) {
        this.userModuleRoleRepository = userModuleRoleRepository;
    }

    public void execute(RevokeUserModuleAccessCommand command) {
        UserModuleRole userModuleRole = userModuleRoleRepository
                .findByUserIdAndModuleId(command.getUserId(), command.getModuleId())
                .orElseThrow(() -> new IllegalArgumentException("USER_MODULE_ROLE_NOT_FOUND"));

        userModuleRoleRepository.deleteByUserIdAndModuleId(userModuleRole);
    }
}
