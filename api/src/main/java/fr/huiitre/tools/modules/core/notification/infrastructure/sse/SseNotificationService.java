package fr.huiitre.tools.modules.core.notification.infrastructure.sse;

import java.io.IOException;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.CopyOnWriteArraySet;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.springframework.web.servlet.mvc.method.annotation.SseEmitter;

import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;

@Service
public class SseNotificationService {

    private static final Logger log = LoggerFactory.getLogger(SseNotificationService.class);
    // Gestion de plusieurs émetteurs par utilisateur (multi-onglets)
    private final Map<Long, Set<SseEmitter>> emitters = new ConcurrentHashMap<>();

    public SseEmitter createEmitter(Long userId) {
        SseEmitter emitter = new SseEmitter(1800000L); // 30 minutes
        
        // Initialisation de la liste des émetteurs pour cet utilisateur si absent
        Set<SseEmitter> userEmitters = emitters.computeIfAbsent(userId, k -> new CopyOnWriteArraySet<>());
        userEmitters.add(emitter);
        
        emitter.onCompletion(() -> removeEmitter(userId, emitter));
        emitter.onTimeout(() -> removeEmitter(userId, emitter));
        emitter.onError((e) -> removeEmitter(userId, emitter));
        
        try {
            emitter.send(SseEmitter.event().name("connect").data("Connected"));
        } catch (IOException e) {
            removeEmitter(userId, emitter);
        }
        
        return emitter;
    }

    private void removeEmitter(Long userId, SseEmitter emitter) {
        Set<SseEmitter> userEmitters = emitters.get(userId);
        if (userEmitters != null) {
            userEmitters.remove(emitter);
            if (userEmitters.isEmpty()) {
                emitters.remove(userId);
            }
        }
    }

    @Scheduled(fixedRate = 20000)
    public void sendHeartbeat() {
        emitters.forEach((userId, userEmitters) -> {
            userEmitters.forEach(emitter -> {
                try {
                    emitter.send(SseEmitter.event().comment("heartbeat"));
                } catch (Exception e) {
                    removeEmitter(userId, emitter);
                }
            });
        });
    }

    public void sendNotification(Long userId, Notification notification) {
        Set<SseEmitter> userEmitters = emitters.get(userId);
        if (userEmitters != null) {
            for (SseEmitter emitter : userEmitters) {
                try {
                    emitter.send(SseEmitter.event()
                        .name("notification")
                        .id(notification.id().toString())
                        .data(notification));
                } catch (Exception e) {
                    removeEmitter(userId, emitter);
                    try {
                        emitter.complete(); 
                    } catch (Exception ignored) {}
                }
            }
        }
    }

    public Set<Long> getConnectedUserIds() {
        return new HashSet<>(emitters.keySet());
    }

    public void broadcastNotification(Notification notification, List<Long> potentialTargetIds) {
        Set<Long> connectedIds = getConnectedUserIds(); // Set pour O(1)
        for (Long userId : potentialTargetIds) {
            if (connectedIds.contains(userId)) {
                sendNotification(userId, notification);
            }
        }
    }
}
