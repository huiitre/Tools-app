package fr.huiitre.tools.modules.core.notification.application.event;

import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.context.event.EventListener;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.notification.application.port.NotificationRepository;
import fr.huiitre.tools.modules.core.notification.application.port.NotificationSenderPort;
import fr.huiitre.tools.modules.core.notification.domain.entity.Notification;
import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.domain.Role;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.infrastructure.RoleHierarchy;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;

@Component
public class NotificationEventListener {

    private final NotificationRepository notificationRepository;
    private final List<NotificationSenderPort> notificationSenders;
    private final UserRepository userRepository;
    private final RoleRepository roleRepository;

    public NotificationEventListener(
            NotificationRepository notificationRepository,
            List<NotificationSenderPort> notificationSenders,
            UserRepository userRepository,
            RoleRepository roleRepository) {
        this.notificationRepository = notificationRepository;
        this.notificationSenders = notificationSenders;
        this.userRepository = userRepository;
        this.roleRepository = roleRepository;
    }

    @Async
    @EventListener
    @Transactional
    public void handleNotificationEvent(NotificationEvent event) {
        // 1. Sauvegarde du message source
        Notification notification = Notification.create(
                event.title(),
                event.body(),
                event.type(),
                event.targetUserId(),
                event.targetRoleId(),
                event.targetModuleId(),
                event.metadata());
        
        Notification savedNotification = notificationRepository.save(notification);

        // 2. Résolution des destinataires potentiels
        List<Long> potentialTargetIds;
        
        if (event.targetUserId() != null) {
            potentialTargetIds = List.of(event.targetUserId());
        } else if (event.targetMinRoleCode() != null) {
            List<String> codes = RoleHierarchy.getCodesAtOrAbove(event.targetMinRoleCode()).stream()
                    .map(RoleCode::name)
                    .toList();
            potentialTargetIds = userRepository.findAllIdsByRoleCodes(codes);
        } else if (event.targetRoleId() != null) {
            potentialTargetIds = userRepository.findAllIdsByRoleId(event.targetRoleId());
        } else if (event.targetModuleId() != null) {
            potentialTargetIds = userRepository.findAllIdsByModuleId(event.targetModuleId());
        } else {
            potentialTargetIds = userRepository.findAllIds();
        }

        // 3. Filtrage : EXCLUSION DU RÔLE TECH
        Set<Long> techUserIds = roleRepository.findByCode(RoleCode.TECH.name())
                .map(Role::getId)
                .map(userRepository::findAllIdsByRoleId)
                .map(HashSet::new)
                .orElse(new HashSet<>());

        List<Long> finalTargetIds = potentialTargetIds.stream()
                .filter(id -> !techUserIds.contains(id))
                .collect(Collectors.toList());

        // 4. Création des entrées individuelles et envoi
        if (!finalTargetIds.isEmpty()) {
            notificationRepository.createUserEntries(savedNotification.id(), finalTargetIds);
            
            // Envoi via tous les transports disponibles (SSE, WebSocket, etc.)
            notificationSenders.forEach(sender -> sender.sendNotification(savedNotification, finalTargetIds));
        }
    }
}
