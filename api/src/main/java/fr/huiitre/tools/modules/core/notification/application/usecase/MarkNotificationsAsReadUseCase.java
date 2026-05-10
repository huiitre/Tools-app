package fr.huiitre.tools.modules.core.notification.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.notification.application.port.NotificationRepository;

@Service
public class MarkNotificationsAsReadUseCase implements SecuredUseCase {

    private final NotificationRepository notificationRepository;
    private final CurrentUserProvider currentUserProvider;

    public MarkNotificationsAsReadUseCase(NotificationRepository notificationRepository, CurrentUserProvider currentUserProvider) {
        this.notificationRepository = notificationRepository;
        this.currentUserProvider = currentUserProvider;
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public void execute(List<Long> notificationIds) {
        Long userId = Long.parseLong(currentUserProvider.getCurrentUserId());
        notificationRepository.markAsRead(userId, notificationIds);
    }
}
