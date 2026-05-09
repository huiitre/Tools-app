package fr.huiitre.tools.modules.core.user.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.api.dto.UserProfileDto;
import fr.huiitre.tools.modules.core.auth.application.exception.UserNotFoundException;
import fr.huiitre.tools.modules.core.role.api.view.RoleView;
import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.user.application.ports.AvatarResolver;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.user.domain.User;
import fr.huiitre.tools.modules.core.user_module.application.ports.UserModuleRoleRepository;
import fr.huiitre.tools.modules.core.user_module.application.view.UserModuleView;

@Service
@Transactional
public class GetCurrentUserProfileUseCase implements SecuredUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;

    private final UserRepository userRepository;
    private final UserRoleRepository userRoleRepository;
    private final UserModuleRoleRepository userModuleRoleRepository;
    private final AvatarResolver avatarResolver;

    public GetCurrentUserProfileUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            UserRepository userRepository,
            UserRoleRepository userRoleRepository,
            UserModuleRoleRepository userModuleRoleRepository,
            AvatarResolver avatarResolver) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.userRepository = userRepository;
        this.userRoleRepository = userRoleRepository;
        this.userModuleRoleRepository = userModuleRoleRepository;
        this.avatarResolver = avatarResolver;
    }

    public UserProfileDto execute() {
        Long userId = authenticatedUserProvider.getUserId();

        User user = userRepository.findById(userId)
                .orElseThrow(() -> new UserNotFoundException("USER_NOT_FOUND"));

        List<RoleView> roles = userRoleRepository.findAllByUserId(userId);

        List<UserModuleView> modules = userModuleRoleRepository.findAllByUserId(userId);

        // * récupération de l'avatarUrl */
        String avatarUrl = avatarResolver.resolve(user);

        return new UserProfileDto(
                user.getId(),
                user.getEmail(),
                user.getName(),
                user.getUserType().name(),
                user.isActive(),
                avatarUrl,
                roles,
                modules);
    }
}