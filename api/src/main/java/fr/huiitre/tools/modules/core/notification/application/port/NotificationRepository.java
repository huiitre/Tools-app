package fr.huiitre.tools.modules.core.notification.application.port;

import java.util.List;

import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;

public interface NotificationRepository {
    List<Notification> findActiveForUser(Long userId);

    void markAsRead(Long userId, List<Long> notificationIds);
    void delete(Long userId, List<Long> notificationIds);
}
