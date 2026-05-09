package fr.huiitre.tools.modules.core.auth.application.usecase;

import java.time.LocalDateTime;
import java.util.Optional;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.EmailSender;
import fr.huiitre.tools.modules.core.auth.application.ports.UserEmailVerificationRepository;
import fr.huiitre.tools.modules.core.user.domain.User;

@Service
@Transactional
public class SendEmailVerificationUseCase {

    private final CreateEmailVerificationUseCase createEmailVerificationUseCase;
    private final EmailSender emailSender;
    private final String frontendBaseUrl;
    private final UserEmailVerificationRepository userEmailVerificationRepository;

    public SendEmailVerificationUseCase(
            CreateEmailVerificationUseCase createEmailVerificationUseCase,
            EmailSender emailSender,
            UserEmailVerificationRepository userEmailVerificationRepository,
            @Value("${app.frontend.base-url}") String frontendBaseUrl) {
        this.createEmailVerificationUseCase = createEmailVerificationUseCase;
        this.emailSender = emailSender;
        this.userEmailVerificationRepository = userEmailVerificationRepository;
        this.frontendBaseUrl = frontendBaseUrl;
    }

    public void execute(Long userId, String email) {

        Optional<LocalDateTime> lastCreatedAtOpt = userEmailVerificationRepository.findLastCreatedAtByUserId(userId);

        if (lastCreatedAtOpt.isPresent()) {
            LocalDateTime lastCreatedAt = lastCreatedAtOpt.get();
            if (lastCreatedAt.plusMinutes(5).isAfter(LocalDateTime.now())) {
                return;
            }
        }

        // 1. Générer token + stocker
        String token = createEmailVerificationUseCase.execute(userId);

        // 2. Construire lien
        String link = frontendBaseUrl + "/auth/verify-email?token=" + token;

        // 3. Envoyer mail
        emailSender.sendEmailVerification(email, link);
    }
}
