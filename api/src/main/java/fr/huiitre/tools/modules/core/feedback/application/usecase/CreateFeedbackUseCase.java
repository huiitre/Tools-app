package fr.huiitre.tools.modules.core.feedback.application.usecase;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.feedback.application.port.FeedbackRepository;

@Service
public class CreateFeedbackUseCase implements SecuredUseCase {

    private final FeedbackRepository feedbackRepository;
    private final CurrentUserProvider currentUserProvider;

    public CreateFeedbackUseCase(FeedbackRepository feedbackRepository, CurrentUserProvider currentUserProvider) {
        this.feedbackRepository = feedbackRepository;
        this.currentUserProvider = currentUserProvider;
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public void execute(String rawMessage) {
        if (rawMessage == null || rawMessage.isBlank()) {
            throw new IllegalArgumentException("Message cannot be empty");
        }

        // Strip HTML tags and limit to 500 chars
        String sanitized = rawMessage.replaceAll("<[^>]*>", "").strip();
        if (sanitized.length() > 500) {
            sanitized = sanitized.substring(0, 500);
        }

        Long userId = Long.parseLong(currentUserProvider.getCurrentUserId());
        feedbackRepository.save(userId, sanitized);
    }
}
