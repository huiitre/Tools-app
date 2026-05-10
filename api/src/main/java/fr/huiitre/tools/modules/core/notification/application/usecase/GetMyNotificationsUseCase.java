package fr.huiitre.tools.modules.core.notification.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.notification.api.view.NotificationView;
import fr.huiitre.tools.modules.core.notification.application.port.NotificationRepository;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;

@Service
public class GetMyNotificationsUseCase implements SecuredUseCase {

    private final NotificationRepository notificationRepository;
    private final CurrentUserProvider currentUserProvider;

    public GetMyNotificationsUseCase(
            NotificationRepository notificationRepository,
            CurrentUserProvider currentUserProvider) {
        this.notificationRepository = notificationRepository;
        this.currentUserProvider = currentUserProvider;
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.READ_ONLY;
    }

    public List<NotificationView> execute() {
        Long userId = Long.parseLong(currentUserProvider.getCurrentUserId());

        return notificationRepository.findActiveForUser(userId).stream()
                .map(NotificationView::fromDomain)
                .toList();
    }
}
