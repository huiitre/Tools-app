package fr.huiitre.tools.modules.core.user.application.usecase;

import fr.huiitre.tools.modules.core.auth.api.dto.UserProfileDto;
import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.user.domain.User;
import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.user_module.application.view.UserModuleView;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class GetUserDetailUseCase implements SecuredUseCase {

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    private final UserRepository userRepository;
    private final UserRoleRepository userRoleRepository;
    private final UserModuleRoleRepository userModuleRoleRepository;

    public GetUserDetailUseCase(
            UserRepository userRepository,
            UserRoleRepository userRoleRepository,
            UserModuleRoleRepository userModuleRoleRepository) {
        this.userRepository = userRepository;
        this.userRoleRepository = userRoleRepository;
        this.userModuleRoleRepository = userModuleRoleRepository;
    }

    public UserProfileDto execute(Long userId) {
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new IllegalArgumentException("USER_NOT_FOUND"));

        List<RoleView> roles = userRoleRepository.findAllByUserId(userId);
        List<UserModuleView> modules = userModuleRoleRepository.findAllByUserId(userId);

        return new UserProfileDto(
                user.getId(),
                user.getEmail(),
                user.getName(),
                user.getUserType().name(),
                user.isActive(),
                null,
                roles,
                modules);
    }
}
