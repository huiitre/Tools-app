package fr.huiitre.tools.modules.core.auth.application.usecase;

import java.security.SecureRandom;
import java.time.LocalDateTime;
import java.util.Base64;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.UserEmailVerificationRepository;

@Service
@Transactional
public class CreateEmailVerificationUseCase {

    private static final int TOKEN_SIZE = 32;
    private static final int EXPIRATION_MINUTES = 30;

    private final UserEmailVerificationRepository userEmailVerificationRepository;
    private final SecureRandom secureRandom = new SecureRandom();

    public CreateEmailVerificationUseCase(
            UserEmailVerificationRepository userEmailVerificationRepository) {
        this.userEmailVerificationRepository = userEmailVerificationRepository;
    }

    public String execute(Long userId) {

        // 1. Invalider les anciennes demandes
        userEmailVerificationRepository.deleteByUserId(userId);

        // 2. Générer un token sécurisé
        String token = generateToken();

        // 3. Expiration
        LocalDateTime expiresAt = LocalDateTime.now().plusMinutes(EXPIRATION_MINUTES);

        // 4. Sauvegarde
        userEmailVerificationRepository.save(
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