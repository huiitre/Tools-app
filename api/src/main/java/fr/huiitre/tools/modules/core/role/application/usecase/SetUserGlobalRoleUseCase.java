package fr.huiitre.tools.modules.core.role.application.usecase;

import fr.huiitre.tools.modules.core.role.application.command.SetUserGlobalRoleCommand;
import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.role.domain.UserRole;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@Transactional
public class SetUserGlobalRoleUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final UserRepository userRepository;
    private final UserRoleRepository userRoleRepository;
    private final RoleRepository roleRepository;

    public SetUserGlobalRoleUseCase(
            UserRepository userRepository,
            UserRoleRepository userRoleRepository,
            RoleRepository roleRepository) {
        this.userRepository = userRepository;
        this.userRoleRepository = userRoleRepository;
        this.roleRepository = roleRepository;
    }

    public void execute(Long userId, SetUserGlobalRoleCommand command) {
        userRepository.findById(userId)
                .orElseThrow(() -> new IllegalArgumentException("USER_NOT_FOUND"));

        long roleId = roleRepository.findById(command.getRoleId())
                .orElseThrow(() -> new IllegalArgumentException("ROLE_NOT_FOUND"))
                .getId();

        userRoleRepository.deleteAllByUserId(userId);
        userRoleRepository.save(new UserRole(userId, roleId));
    }
}
