package fr.huiitre.tools.modules.core.notification.application.usecase;

import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.notification.application.event.NotificationEvent;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;

@Service
public class SendNotificationUseCase implements SecuredUseCase {

    private final ApplicationEventPublisher eventPublisher;

    public SendNotificationUseCase(ApplicationEventPublisher eventPublisher) {
        this.eventPublisher = eventPublisher;
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public void execute(SendNotificationCommand command) {
        NotificationEvent event = new NotificationEvent(
            command.title(),
            command.body(),
            command.type(),
            command.targetUserId(),
            command.targetRoleId(),
            command.targetModuleId(),
            command.metadata()
        );
        eventPublisher.publishEvent(event);
    }
}
