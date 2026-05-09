package fr.huiitre.tools.modules.core.auth.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.PasswordHasher;
import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserCredentialsRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.auth.application.command.LoginUserCommand;
import fr.huiitre.tools.modules.core.auth.application.exception.InvalidCredentialsException;
import fr.huiitre.tools.modules.core.auth.application.exception.UserDisabledException;
import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;
import fr.huiitre.tools.modules.core.user.domain.User;

@Service
@Transactional
public class LoginUserUseCase {

    private final UserRepository userRepository;
    private final UserCredentialsRepository credentialsRepository;
    private final UserAuthProviderRepository userAuthProviderRepository;
    private final PasswordHasher passwordHasher;

    public LoginUserUseCase(
            UserRepository userRepository,
            UserCredentialsRepository credentialsRepository,
            UserAuthProviderRepository userAuthProviderRepository,
            PasswordHasher passwordHasher) {
        this.userRepository = userRepository;
        this.credentialsRepository = credentialsRepository;
        this.userAuthProviderRepository = userAuthProviderRepository;
        this.passwordHasher = passwordHasher;
    }

    public User execute(LoginUserCommand command) {

        /*
         * Charger l’utilisateur à partir de l’email.
         * - L’email est l’identifiant fonctionnel.
         * - Si l’utilisateur n’existe pas → échec login.
         * - Message volontairement générique (pas d’info leak).
         */
        User user = userRepository
                .findByEmail(command.getEmail())
                .orElseThrow(() -> new InvalidCredentialsException());

        /*
         * Utilisateur désactivé
         */
        if (!user.isActive()) {
            throw new UserDisabledException("Utilisateur désactivé");
        }

        /*
         * Vérification du PROVIDER de l'utilisateur
         */
        boolean hasPasswordProvider = userAuthProviderRepository.existsByUserIdAndProvider(
                user.getId(),
                AuthProvider.PASSWORD);

        if (!hasPasswordProvider) {
            throw new InvalidCredentialsException();
        }

        /*
         * Récupérer le hash du mot de passe.
         * - Séparé de User (table user_credentials).
         * - Un user peut exister sans credentials (OAuth plus tard).
         */
        String passwordHash = credentialsRepository
                .findPasswordHashByUserId(user.getId())
                .orElseThrow(() -> new InvalidCredentialsException());

        /*
         * Vérifier le mot de passe.
         * - Le use case ne connaît pas bcrypt.
         * - Il délègue au port PasswordHasher.
         */
        boolean valid = passwordHasher.matches(
                command.getPassword(),
                passwordHash);

        if (!valid) {
            throw new InvalidCredentialsException();
        }

        /*
         * Login réussi.
         * - Le use case retourne l’identité authentifiée.
         * - Pas de JWT ici.
         * - Pas de HTTP ici.
         */
        return user;
    }
}
