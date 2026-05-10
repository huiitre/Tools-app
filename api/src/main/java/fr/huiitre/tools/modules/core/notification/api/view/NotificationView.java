package fr.huiitre.tools.modules.core.notification.api.view;

import java.time.LocalDateTime;

import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;
import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;

public record NotificationView(
    Long id,
    String title,
    String body,
    NotificationType type,
    String metadata,
    LocalDateTime createdAt,
    boolean read
) {
    public static NotificationView fromDomain(Notification notification) {
        return new NotificationView(
            notification.id(),
            notification.title(),
            notification.body(),
            notification.type(),
            notification.metadata(),
            notification.createdAt(),
            notification.read()
        );
    }
}
