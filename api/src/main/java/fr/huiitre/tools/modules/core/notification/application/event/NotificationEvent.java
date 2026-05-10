package fr.huiitre.tools.modules.core.notification.application.event;

import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;

public record NotificationEvent(
    String title,
    String body,
    NotificationType type,
    Long targetUserId,
    Long targetRoleId,
    Long targetModuleId,
    String metadata
) {
    public static NotificationEvent global(String title, String body, NotificationType type) {
        return new NotificationEvent(title, body, type, null, null, null, null);
    }

    public static NotificationEvent user(Long userId, String title, String body, NotificationType type) {
        return new NotificationEvent(title, body, type, userId, null, null, null);
    }

    public static NotificationEvent module(Long moduleId, String title, String body, NotificationType type) {
        return new NotificationEvent(title, body, type, null, null, moduleId, null);
    }
}
