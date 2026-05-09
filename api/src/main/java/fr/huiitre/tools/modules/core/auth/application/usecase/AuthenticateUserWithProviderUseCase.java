package fr.huiitre.tools.modules.core.auth.application.usecase;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.auth.application.command.RegisterUserCommand;
import fr.huiitre.tools.modules.core.auth.application.exception.RegisterException;
import fr.huiitre.tools.modules.core.auth.application.exception.UserDisabledException;
import fr.huiitre.tools.modules.core.role.domain.Role;
import fr.huiitre.tools.modules.core.role.domain.UserRole;
import fr.huiitre.tools.modules.core.user.domain.AvatarSource;
import fr.huiitre.tools.modules.core.user.domain.User;
import fr.huiitre.tools.modules.core.user.domain.UserType;
import jakarta.transaction.Transactional;

@Service
@Transactional
public class AuthenticateUserWithProviderUseCase {

    private final UserRepository userRepository;
    private final UserAuthProviderRepository userAuthProviderRepository;
    private final UserRoleRepository userRoleRepository;
    private final RoleRepository roleRepository;

    public AuthenticateUserWithProviderUseCase(
            UserRepository userRepository,
            UserAuthProviderRepository userAuthProviderRepository,
            UserRoleRepository userRoleRepository,
            RoleRepository roleRepository) {
        this.userRepository = userRepository;
        this.userAuthProviderRepository = userAuthProviderRepository;
        this.userRoleRepository = userRoleRepository;
        this.roleRepository = roleRepository;
    }

    public User execute(RegisterUserCommand command) {

        // 1. Vérifier si un lien provider existe déjà
        return userAuthProviderRepository
                .findUserIdByProviderAndProviderUserId(
                        command.getProvider(),
                        command.getProviderUserId())
                .flatMap(userRepository::findById)
                .map(user -> {
                    ensureActive(user);

                    if (command.getPicture() != null) {
                        userAuthProviderRepository.updateProviderAvatarUrl(
                                user.getId(),
                                command.getProvider(),
                                command.getPicture());
                    }
                    return user;
                })
                .orElseGet(() -> authenticateOrRegisterByEmail(command));
    }

    private User authenticateOrRegisterByEmail(RegisterUserCommand command) {

        if (userRepository.findByEmail(command.getEmail()).isPresent()) {
            throw new RegisterException("Un compte existe déjà avec cette adresse email.");
        }

        return registerNewUser(command);
    }

    private User registerNewUser(RegisterUserCommand command) {

        // 2. Création user
        User user = new User(
                command.getName(),
                command.getEmail(),
                UserType.HUMAN,
                AvatarSource.valueOf(command.getProvider().name()));

        user.setIsActive(true);

        userRepository.save(user);

        // 3. Lien provider
        userAuthProviderRepository.save(
                user.getId(),
                command.getProvider(),
                command.getProviderUserId(),
                command.getEmail(),
                command.getPicture());

        // 4. Rôle USER
        Role role = roleRepository.findByCode("USER")
                .orElseThrow(() -> new RegisterException(
                        "La configuration du compte utilisateur est incomplète. Veuillez contacter le support."));

        userRoleRepository.save(new UserRole(user.getId(), role.getId()));

        return user;
    }

    private User ensureActive(User user) {
        if (!user.isActive()) {
            throw new UserDisabledException("Utilisateur désactivé");
        }
        return user;
    }
}
