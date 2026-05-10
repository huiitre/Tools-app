package fr.huiitre.tools.modules.core.notification.api;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;

import fr.huiitre.tools.modules.core.notification.api.view.NotificationView;
import fr.huiitre.tools.modules.core.notification.application.usecase.DeleteNotificationsUseCase;
import fr.huiitre.tools.modules.core.notification.application.usecase.GetMyNotificationsUseCase;
import fr.huiitre.tools.modules.core.notification.application.usecase.MarkNotificationsAsReadUseCase;
import fr.huiitre.tools.modules.core.notification.application.usecase.SendNotificationCommand;
import fr.huiitre.tools.modules.core.notification.application.usecase.SendNotificationUseCase;
import fr.huiitre.tools.modules.core.notification.application.usecase.StreamNotificationsUseCase;
import fr.huiitre.tools.modules.core.notification.infrastructure.sse.SseNotificationService;
import fr.huiitre.tools.modules.core.security.application.ports.CurrentUserProvider;

@RestController
@RequestMapping("/notifications")
public class NotificationController {

    private final GetMyNotificationsUseCase getMyNotificationsUseCase;
    private final MarkNotificationsAsReadUseCase markNotificationsAsReadUseCase;
    private final DeleteNotificationsUseCase deleteNotificationsUseCase;
    private final SendNotificationUseCase sendNotificationUseCase;
    private final StreamNotificationsUseCase streamNotificationsUseCase;

    public NotificationController(
            GetMyNotificationsUseCase getMyNotificationsUseCase,
            MarkNotificationsAsReadUseCase markNotificationsAsReadUseCase,
            DeleteNotificationsUseCase deleteNotificationsUseCase,
            SendNotificationUseCase sendNotificationUseCase,
            StreamNotificationsUseCase streamNotificationsUseCase) {
        this.getMyNotificationsUseCase = getMyNotificationsUseCase;
        this.markNotificationsAsReadUseCase = markNotificationsAsReadUseCase;
        this.deleteNotificationsUseCase = deleteNotificationsUseCase;
        this.sendNotificationUseCase = sendNotificationUseCase;
        this.streamNotificationsUseCase = streamNotificationsUseCase;
    }

    @GetMapping
    public List<NotificationView> getMyNotifications() {
        return getMyNotificationsUseCase.execute();
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public void sendNotification(@RequestBody SendNotificationCommand command) {
        sendNotificationUseCase.execute(command);
    }

    @GetMapping(path = "/stream", produces = MediaType.TEXT_EVENT_STREAM_VALUE)
    public SseEmitter streamNotifications() {
        return streamNotificationsUseCase.execute();
    }

    @PatchMapping("/read")
    public void markAsRead(@RequestParam(required = false) List<Long> ids) {
        markNotificationsAsReadUseCase.execute(ids);
    }

    @DeleteMapping
    public void delete(@RequestParam(required = false) List<Long> ids) {
        deleteNotificationsUseCase.execute(ids);
    }
}
