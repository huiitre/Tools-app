package fr.huiitre.tools.modules.core.notification.application.usecase;

import org.springframework.stereotype.Service;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.notification.infrastructure.sse.SseNotificationService;

@Service
public class StreamNotificationsUseCase implements SecuredUseCase {

    private final SseNotificationService sseNotificationService;
    private final CurrentUserProvider currentUserProvider;

    public StreamNotificationsUseCase(SseNotificationService sseNotificationService, CurrentUserProvider currentUserProvider) {
        this.sseNotificationService = sseNotificationService;
        this.currentUserProvider = currentUserProvider;
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public SseEmitter execute() {
        Long userId = Long.parseLong(currentUserProvider.getCurrentUserId());
        return sseNotificationService.createEmitter(userId);
    }
}
