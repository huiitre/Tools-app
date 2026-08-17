package fr.huiitre.tools.modules.core.notification.application.port;

import java.util.Optional;

import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;

public interface ApiCoreNotificationPort {

    Optional<Long> publish(
            String title,
            String body,
            NotificationType type,
            Long targetUserId,
            RoleCode targetMinRole,
            String metadata);
}
