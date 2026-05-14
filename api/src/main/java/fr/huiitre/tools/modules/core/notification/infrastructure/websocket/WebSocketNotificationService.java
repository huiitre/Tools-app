package fr.huiitre.tools.modules.core.notification.infrastructure.websocket;

import fr.huiitre.tools.modules.core.notification.application.port.NotificationSenderPort;
import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
public class WebSocketNotificationService implements NotificationSenderPort {

    private static final Logger log = LoggerFactory.getLogger(WebSocketNotificationService.class);
    private final SimpMessagingTemplate messagingTemplate;

    public WebSocketNotificationService(SimpMessagingTemplate messagingTemplate) {
        this.messagingTemplate = messagingTemplate;
    }

    @Override
    public void sendNotification(Notification notification, List<Long> targetUserIds) {
        for (Long userId : targetUserIds) {
            log.info("Sending WebSocket notification to user {}: {}", userId, notification.title());
            // La destination finale sera /user/{userId}/queue/core/notifications
            // Côté front, on s'abonne à /user/queue/core/notifications
            messagingTemplate.convertAndSendToUser(
                    userId.toString(),
                    "/queue/core/notifications",
                    notification
            );
        }
    }
}
