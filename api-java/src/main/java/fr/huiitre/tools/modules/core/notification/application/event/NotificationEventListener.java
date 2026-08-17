package fr.huiitre.tools.modules.core.notification.application.event;

import org.springframework.context.event.EventListener;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Component;

import fr.huiitre.tools.modules.core.notification.application.port.ApiCoreNotificationPort;

// Persistance, résolution des destinataires et push délégués à l'API Core (ApiCoreNotificationPort).
@Component
public class NotificationEventListener {

    private final ApiCoreNotificationPort apiCoreNotificationPort;

    public NotificationEventListener(ApiCoreNotificationPort apiCoreNotificationPort) {
        this.apiCoreNotificationPort = apiCoreNotificationPort;
    }

    @Async
    @EventListener
    public void handleNotificationEvent(NotificationEvent event) {
        if (event.targetUserId() == null && event.targetMinRoleCode() == null && event.targetModuleId() == null) {
            // Ciblage par rôle exact ou global : aucun appelant, non supporté par l'API Core.
            return;
        }

        apiCoreNotificationPort.publish(
                event.title(),
                event.body(),
                event.type(),
                event.targetUserId(),
                event.targetMinRoleCode(),
                event.targetModuleId(),
                event.metadata());
    }
}
