package fr.huiitre.tools.modules.core.auth.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.exception.UserNotFoundException;
import fr.huiitre.tools.modules.core.auth.application.ports.PasswordHasher;
import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;
import fr.huiitre.tools.modules.core.security.application.ports.AuthenticatedUserProvider;
import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserCredentialsRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.user.domain.User;

@Service
@Transactional
public class SetUserPasswordUseCase {

    private final AuthenticatedUserProvider authenticatedUserProvider;
    private final UserRepository userRepository;
    private final UserCredentialsRepository credentialsRepository;
    private final UserAuthProviderRepository authProviderRepository;
    private final PasswordHasher passwordHasher;

    public SetUserPasswordUseCase(
            AuthenticatedUserProvider authenticatedUserProvider,
            UserRepository userRepository,
            UserCredentialsRepository credentialsRepository,
            UserAuthProviderRepository authProviderRepository,
            PasswordHasher passwordHasher) {
        this.authenticatedUserProvider = authenticatedUserProvider;
        this.userRepository = userRepository;
        this.credentialsRepository = credentialsRepository;
        this.authProviderRepository = authProviderRepository;
        this.passwordHasher = passwordHasher;
    }

    public void execute(String newPassword) {
        Long userId = authenticatedUserProvider.getUserId();

        User user = userRepository.findById(userId)
                .orElseThrow(() -> new UserNotFoundException("Utilisateur introuvable"));

        String hash = passwordHasher.hash(newPassword);

        boolean hasCredential = credentialsRepository.findPasswordHashByUserId(userId).isPresent();

        if (hasCredential) {
            credentialsRepository.updatePassword(userId, hash);
        } else {
            credentialsRepository.save(userId, hash);
            authProviderRepository.save(
                    userId,
                    AuthProvider.PASSWORD,
                    user.getEmail(),
                    user.getEmail(),
                    null);
        }
    }
}