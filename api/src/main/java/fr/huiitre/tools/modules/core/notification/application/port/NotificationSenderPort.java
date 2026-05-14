package fr.huiitre.tools.modules.core.notification.application.port;

import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;
import java.util.List;

public interface NotificationSenderPort {
    /**
     * Envoie une notification aux utilisateurs ciblés.
     */
    void sendNotification(Notification notification, List<Long> targetUserIds);
}
