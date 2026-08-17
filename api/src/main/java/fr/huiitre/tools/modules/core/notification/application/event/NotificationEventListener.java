package fr.huiitre.tools.modules.core.notification.application.event;

import java.time.LocalDateTime;
import java.util.HashSet;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Collectors;

import org.springframework.context.event.EventListener;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.notification.application.port.ApiCoreNotificationPort;
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
    private final ApiCoreNotificationPort apiCoreNotificationPort;
    private final List<NotificationSenderPort> notificationSenders;
    private final UserRepository userRepository;
    private final RoleRepository roleRepository;

    public NotificationEventListener(
            NotificationRepository notificationRepository,
            ApiCoreNotificationPort apiCoreNotificationPort,
            List<NotificationSenderPort> notificationSenders,
            UserRepository userRepository,
            RoleRepository roleRepository) {
        this.notificationRepository = notificationRepository;
        this.apiCoreNotificationPort = apiCoreNotificationPort;
        this.notificationSenders = notificationSenders;
        this.userRepository = userRepository;
        this.roleRepository = roleRepository;
    }

    // Ciblage par utilisateur ou rôle minimum : persistance déléguée à l'API Core, push local inchangé.
    // Ciblage par rôle exact ou module (aucun appelant) : écriture locale inchangée.
    @Async
    @EventListener
    @Transactional
    public void handleNotificationEvent(NotificationEvent event) {
        if (event.targetUserId() != null || event.targetMinRoleCode() != null) {
            handleViaApiCore(event);
        } else {
            handleLegacyLocalWrite(event);
        }
    }

    private void handleViaApiCore(NotificationEvent event) {
        List<Long> potentialTargetIds = event.targetUserId() != null
                ? List.of(event.targetUserId())
                : userRepository.findAllIdsByRoleCodes(
                        RoleHierarchy.getCodesAtOrAbove(event.targetMinRoleCode()).stream()
                                .map(RoleCode::name)
                                .toList());

        List<Long> finalTargetIds = excludingTech(potentialTargetIds);
        if (finalTargetIds.isEmpty()) {
            return;
        }

        Optional<Long> notificationId = apiCoreNotificationPort.publish(
                event.title(),
                event.body(),
                event.type(),
                event.targetUserId(),
                event.targetMinRoleCode(),
                event.metadata());

        notificationId.ifPresent(id -> {
            Notification notification = new Notification(
                    id,
                    event.title(),
                    event.body(),
                    event.type(),
                    event.targetUserId(),
                    event.targetRoleId(),
                    event.targetModuleId(),
                    event.metadata(),
                    LocalDateTime.now(),
                    false);

            notificationSenders.forEach(sender -> sender.sendNotification(notification, finalTargetIds));
        });
    }

    private void handleLegacyLocalWrite(NotificationEvent event) {
        Notification notification = Notification.create(
                event.title(),
                event.body(),
                event.type(),
                event.targetUserId(),
                event.targetRoleId(),
                event.targetModuleId(),
                event.metadata());

        Notification savedNotification = notificationRepository.save(notification);

        List<Long> potentialTargetIds = event.targetRoleId() != null
                ? userRepository.findAllIdsByRoleId(event.targetRoleId())
                : event.targetModuleId() != null
                        ? userRepository.findAllIdsByModuleId(event.targetModuleId())
                        : userRepository.findAllIds();

        List<Long> finalTargetIds = excludingTech(potentialTargetIds);

        if (!finalTargetIds.isEmpty()) {
            notificationRepository.createUserEntries(savedNotification.id(), finalTargetIds);
            notificationSenders.forEach(sender -> sender.sendNotification(savedNotification, finalTargetIds));
        }
    }

    private List<Long> excludingTech(List<Long> potentialTargetIds) {
        Set<Long> techUserIds = roleRepository.findByCode(RoleCode.TECH.name())
                .map(Role::getId)
                .map(userRepository::findAllIdsByRoleId)
                .map(HashSet::new)
                .orElse(new HashSet<>());

        return potentialTargetIds.stream()
                .filter(id -> !techUserIds.contains(id))
                .collect(Collectors.toList());
    }
}
