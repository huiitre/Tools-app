package fr.huiitre.tools.modules.core.notification.domain.entity;

import java.time.LocalDateTime;

public record Notification(
    Long id,
    String title,
    String body,
    NotificationType type,
    Long targetUserId,
    Long targetRoleId,
    Long targetModuleId,
    String metadata,
    LocalDateTime createdAt,
    boolean read
) {
    public static Notification create(
        String title,
        String body,
        NotificationType type,
        Long targetUserId,
        Long targetRoleId,
        Long targetModuleId,
        String metadata
    ) {
        return new Notification(
            null,
            title,
            body,
            type,
            targetUserId,
            targetRoleId,
            targetModuleId,
            metadata,
            LocalDateTime.now(),
            false
        );
    }
}
