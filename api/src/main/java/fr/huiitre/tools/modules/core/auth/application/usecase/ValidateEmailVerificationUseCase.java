package fr.huiitre.tools.modules.core.auth.application.usecase;

import java.time.LocalDateTime;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.UserEmailVerificationRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.auth.application.exception.InvalidEmailVerificationTokenException;
import fr.huiitre.tools.modules.core.user.domain.User;

@Service
@Transactional
public class ValidateEmailVerificationUseCase {

    private final UserEmailVerificationRepository emailVerificationRepository;
    private final UserRepository userRepository;

    public ValidateEmailVerificationUseCase(
            UserEmailVerificationRepository emailVerificationRepository,
            UserRepository userRepository) {
        this.emailVerificationRepository = emailVerificationRepository;
        this.userRepository = userRepository;
    }

    public void execute(String token) {

        Long userId = emailVerificationRepository
                .findUserIdByValidToken(token, LocalDateTime.now())
                .orElseThrow(() -> new InvalidEmailVerificationTokenException());

        User user = userRepository
                .findById(userId)
                .orElseThrow(() -> new InvalidEmailVerificationTokenException());

        if (!user.isActive()) {
            user.setIsActive(true);
            userRepository.save(user);
        }

        emailVerificationRepository.deleteByUserId(userId);
    }
}
