package fr.huiitre.tools.modules.core.auth.application.usecase;

import java.security.SecureRandom;
import java.time.LocalDateTime;
import java.util.Base64;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.UserPasswordResetRepository;

@Service
@Transactional
public class CreatePasswordResetUseCase {

    private static final int TOKEN_SIZE = 32;
    private static final int EXPIRATION_MINUTES = 30;

    private final UserPasswordResetRepository userPasswordResetRepository;
    private final SecureRandom secureRandom = new SecureRandom();

    public CreatePasswordResetUseCase(
            UserPasswordResetRepository userPasswordResetRepository) {
        this.userPasswordResetRepository = userPasswordResetRepository;
    }

    public String execute(Long userId) {

        userPasswordResetRepository.deleteByUserId(userId);

        String token = generateToken();

        LocalDateTime expiresAt = LocalDateTime.now().plusMinutes(EXPIRATION_MINUTES);

        userPasswordResetRepository.save(
                userId,
                token,
                expiresAt);

        return token;
    }

    private String generateToken() {
        byte[] bytes = new byte[TOKEN_SIZE];
        secureRandom.nextBytes(bytes);
        return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
    }
}