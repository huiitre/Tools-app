package fr.huiitre.tools.modules.core.notification.domain.entity;

import java.time.LocalDateTime;

public record UserNotification(
    Long userId,
    Long notificationId,
    boolean read,
    LocalDateTime readAt,
    boolean deleted
) {
}
