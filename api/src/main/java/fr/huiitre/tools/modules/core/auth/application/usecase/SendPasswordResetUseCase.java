package fr.huiitre.tools.modules.core.auth.application.usecase;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.EmailSender;

@Service
@Transactional
public class SendPasswordResetUseCase {

    private final CreatePasswordResetUseCase createPasswordResetUseCase;
    private final EmailSender emailSender;
    private final String frontendBaseUrl;

    public SendPasswordResetUseCase(
            CreatePasswordResetUseCase createPasswordResetUseCase,
            EmailSender emailSender,
            @Value("${app.frontend.base-url}") String frontendBaseUrl) {
        this.createPasswordResetUseCase = createPasswordResetUseCase;
        this.emailSender = emailSender;
        this.frontendBaseUrl = frontendBaseUrl;
    }

    public void execute(Long userId, String email) {

        String token = createPasswordResetUseCase.execute(userId);

        String link = frontendBaseUrl + "/auth/reset-password?token=" + token;

        emailSender.sendPasswordReset(email, link);
    }
}