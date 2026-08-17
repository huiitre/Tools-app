package fr.huiitre.tools.modules.core.feedback.application.usecase;

import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.notification.application.event.NotificationEvent;
import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.feedback.application.port.FeedbackRepository;

@Service
public class CreateFeedbackUseCase implements SecuredUseCase {

    private final FeedbackRepository feedbackRepository;
    private final CurrentUserProvider currentUserProvider;
    private final ApplicationEventPublisher eventPublisher;

    public CreateFeedbackUseCase(FeedbackRepository feedbackRepository, CurrentUserProvider currentUserProvider, ApplicationEventPublisher eventPublisher) {
        this.feedbackRepository = feedbackRepository;
        this.currentUserProvider = currentUserProvider;
        this.eventPublisher = eventPublisher;
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

        eventPublisher.publishEvent(NotificationEvent.minRole(
            RoleCode.ADMIN,
            "Nouveau feedback",
            sanitized.length() > 100 ? sanitized.substring(0, 100) + "…" : sanitized,
            NotificationType.INFO
        ));
    }
}
