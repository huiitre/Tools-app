package fr.huiitre.tools.modules.core.auth.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.user.application.ports.UserAuthProviderRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;

@Service
@Transactional
public class RequestPasswordResetUseCase {

    private final UserRepository userRepository;
    private final UserAuthProviderRepository userAuthProviderRepository;
    private final SendPasswordResetUseCase sendPasswordResetUseCase;

    public RequestPasswordResetUseCase(
            UserRepository userRepository,
            UserAuthProviderRepository userAuthProviderRepository,
            SendPasswordResetUseCase sendPasswordResetUseCase) {
        this.userRepository = userRepository;
        this.userAuthProviderRepository = userAuthProviderRepository;
        this.sendPasswordResetUseCase = sendPasswordResetUseCase;
    }

    public void execute(String email) {

        userRepository.findByEmail(email)
                .filter(user -> userAuthProviderRepository.existsByUserIdAndProvider(
                        user.getId(),
                        AuthProvider.PASSWORD))
                .ifPresent(user -> sendPasswordResetUseCase.execute(
                        user.getId(),
                        user.getEmail()));
    }
}