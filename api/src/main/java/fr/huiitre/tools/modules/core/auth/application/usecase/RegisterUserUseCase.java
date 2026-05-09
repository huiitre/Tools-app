package fr.huiitre.tools.modules.core.auth.application.usecase;

import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.PasswordHasher;
import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.application.ports.UserRoleRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserCredentialsRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.auth.application.command.RegisterUserCommand;
import fr.huiitre.tools.modules.core.auth.application.exception.RegisterException;
import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;
import fr.huiitre.tools.modules.core.role.domain.Role;
import fr.huiitre.tools.modules.core.role.domain.UserRole;
import fr.huiitre.tools.modules.core.user.domain.AvatarSource;
import fr.huiitre.tools.modules.core.user.domain.User;
import fr.huiitre.tools.modules.core.user.domain.UserType;

@Service
@Transactional
public class RegisterUserUseCase {

    private static final Logger logger = LoggerFactory.getLogger(RegisterUserUseCase.class);

    private final UserRepository userRepository;
    private final UserCredentialsRepository userCredentialsRepository;
    private final UserAuthProviderRepository userAuthProviderRepository;
    private final UserRoleRepository userRoleRepository;
    private final RoleRepository roleRepository;
    private final PasswordHasher passwordHasher;

    public RegisterUserUseCase(
            UserRepository userRepository,
            UserCredentialsRepository userCredentialsRepository,
            UserAuthProviderRepository userAuthProviderRepository,
            UserRoleRepository userRoleRepository,
            RoleRepository roleRepository,
            PasswordHasher passwordHasher) {
        this.userRepository = userRepository;
        this.userCredentialsRepository = userCredentialsRepository;
        this.userAuthProviderRepository = userAuthProviderRepository;
        this.userRoleRepository = userRoleRepository;
        this.roleRepository = roleRepository;
        this.passwordHasher = passwordHasher;
    }

    public User execute(RegisterUserCommand command) {

        /*
         * ===============================
         * VALIDATIONS
         * ===============================
         */
        if (command.getName() == null || command.getName().isBlank()) {
            throw new RegisterException("Le nom est obligatoire.");
        }

        if (command.getEmail() == null || command.getEmail().isBlank()) {
            throw new RegisterException("L’adresse email est obligatoire.");
        }

        if (command.getPassword() == null || command.getPassword().isBlank()) {
            throw new RegisterException("Le mot de passe est obligatoire.");
        }

        /*
         * ===============================
         * REGLES METIER
         * ===============================
         * Si l'email existe déjà et compte actif → REGISTER INTERDIT
         */
        Optional<User> existingUserOpt = userRepository.findByEmail(command.getEmail());
        if (existingUserOpt.isPresent() && existingUserOpt.get().isActive()) {
            throw new RegisterException("Un compte existe déjà avec cette adresse email.");
        } else if (existingUserOpt.isPresent() && !existingUserOpt.get().isActive()) {
            return existingUserOpt.get();
        }

        /*
         * ===============================
         * CREATION UTILISATEUR
         * ===============================
         */
        User user = new User(
            command.getName(),
            command.getEmail(),
            UserType.HUMAN,
            AvatarSource.PASSWORD
        );

        userRepository.save(user);

        /*
         * ===============================
         * CREDENTIALS
         * ===============================
         */
        String passwordHash = passwordHasher.hash(command.getPassword());

        userCredentialsRepository.save(
                user.getId(),
                passwordHash);

        /*
         * ===============================
         * AUTH PROVIDER : PASSWORD
         * ===============================
         */
        userAuthProviderRepository.save(
                user.getId(),
                AuthProvider.PASSWORD,
                command.getEmail(), // provider_user_id
                command.getEmail(), // provider_email (indicatif)
                null // provider_avatar_url
        );

        /*
         * ===============================
         * ROLE PAR DEFAUT
         * ===============================
         */
        Role role = roleRepository.findByCode("USER")
                .orElseThrow(() -> new RegisterException(
                        "La configuration du compte utilisateur est incomplète. Veuillez contacter le support."));

        userRoleRepository.save(new UserRole(user.getId(), role.getId()));

        return user;
    }
}
