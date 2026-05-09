package fr.huiitre.tools.modules.core.auth.application.usecase;

import java.time.LocalDateTime;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.auth.application.ports.PasswordHasher;
import fr.huiitre.tools.modules.core.auth.application.ports.UserPasswordResetRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserCredentialsRepository;
import fr.huiitre.tools.modules.core.auth.application.exception.InvalidPasswordResetTokenException;
import jakarta.transaction.Transactional;

@Service
@Transactional
public class ValidatePasswordResetUseCase {

    private final UserPasswordResetRepository userPasswordResetRepository;
    private final UserCredentialsRepository userCredentialsRepository;
    private final PasswordHasher passwordHasher;

    public ValidatePasswordResetUseCase(
            UserPasswordResetRepository userPasswordResetRepository,
            UserCredentialsRepository userCredentialsRepository,
            PasswordHasher passwordHasher) {
        this.userPasswordResetRepository = userPasswordResetRepository;
        this.userCredentialsRepository = userCredentialsRepository;
        this.passwordHasher = passwordHasher;
    }

    public void execute(String token, String newPassword) {

        if (newPassword == null || newPassword.isBlank()) {
            throw new RuntimeException("Le mot de passe est obligatoire.");
        }

        Long userId = userPasswordResetRepository
                .findUserIdByValidToken(token, LocalDateTime.now())
                .orElseThrow(() -> new InvalidPasswordResetTokenException(
                        "Lien de réinitialisation du mot de passe invalide ou expiré."));

        String passwordHash = passwordHasher.hash(newPassword);

        userCredentialsRepository.updatePassword(
                userId,
                passwordHash);

        userPasswordResetRepository.deleteByUserId(userId);
    }
}
