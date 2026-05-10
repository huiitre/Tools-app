package fr.huiitre.tools.modules.core.notification.application.usecase;

import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;

public record SendNotificationCommand(
    String title,
    String body,
    NotificationType type,
    Long targetUserId,
    Long targetRoleId,
    Long targetModuleId,
    String metadata
) {
}
