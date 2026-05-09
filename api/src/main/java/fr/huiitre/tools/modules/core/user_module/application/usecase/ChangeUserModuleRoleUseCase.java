package fr.huiitre.tools.modules.core.user_module.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.user_module.application.command.ChangeUserModuleRoleCommand;
import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.role.domain.Role;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.user_module.domain.UserModuleRole;

@Service
@Transactional
public class ChangeUserModuleRoleUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final UserModuleRoleRepository userModuleRoleRepository;
    private final RoleRepository roleRepository;

    public ChangeUserModuleRoleUseCase(
            UserModuleRoleRepository userModuleRoleRepository,
            RoleRepository roleRepository) {
        this.userModuleRoleRepository = userModuleRoleRepository;
        this.roleRepository = roleRepository;
    }

    public void execute(ChangeUserModuleRoleCommand command) {

        // * est ce que l'utilisateur pour le module existe */
        UserModuleRole userModuleRole = userModuleRoleRepository
                .findByUserIdAndModuleId(command.getUserId(), command.getModuleId())
                .orElseThrow(() -> new IllegalArgumentException("USER_MODULE_ROLE_NOT_FOUND"));

        // * est ce que le rôle demandé existe */
        Role role = roleRepository.findById(command.getRoleId())
                .orElseThrow(() -> new IllegalArgumentException("ROLE_NOT_FOUND"));

        userModuleRole.changeRole(role.getId());

        userModuleRoleRepository.updateRoleId(userModuleRole);
    }
}
