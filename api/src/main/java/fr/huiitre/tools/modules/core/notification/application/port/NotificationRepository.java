package fr.huiitre.tools.modules.core.notification.application.port;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;

public interface NotificationRepository {
    Notification save(Notification notification);
    Optional<Notification> findById(Long id);
    List<Notification> findActiveForUser(Long userId);
    
    void markAsRead(Long userId, List<Long> notificationIds);
    void delete(Long userId, List<Long> notificationIds);

    void createUserEntries(Long notificationId, List<Long> userIds);
}
