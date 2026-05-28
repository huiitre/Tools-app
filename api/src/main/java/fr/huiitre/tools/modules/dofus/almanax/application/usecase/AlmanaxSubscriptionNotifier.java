package fr.huiitre.tools.modules.dofus.almanax.application.usecase;

import java.time.LocalDate;
import java.util.List;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.notification.application.event.NotificationEvent;
import fr.huiitre.tools.modules.core.notification.domain.entity.NotificationType;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxRepository;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxSubscriptionRepository;
import fr.huiitre.tools.modules.dofus.almanax.domain.Almanax;
import fr.huiitre.tools.modules.dofus.almanax.domain.DatePattern;

@Service
public class AlmanaxSubscriptionNotifier {

    private static final Logger log = LoggerFactory.getLogger(AlmanaxSubscriptionNotifier.class);

    private final AlmanaxRepository almanaxRepository;
    private final AlmanaxSubscriptionRepository subscriptionRepository;
    private final ApplicationEventPublisher eventPublisher;

    public AlmanaxSubscriptionNotifier(
            AlmanaxRepository almanaxRepository,
            AlmanaxSubscriptionRepository subscriptionRepository,
            ApplicationEventPublisher eventPublisher) {
        this.almanaxRepository = almanaxRepository;
        this.subscriptionRepository = subscriptionRepository;
        this.eventPublisher = eventPublisher;
    }

    public void processToday() {
        LocalDate today = LocalDate.now();
        List<Almanax> allAlmanax = almanaxRepository.findAll();

        Almanax todaysAlmanax = findBestAlmanaxForDate(today, allAlmanax);
        if (todaysAlmanax == null) {
            log.info("No almanax found for today ({}), skipping notifications", today);
            return;
        }

        List<Long> userIds = subscriptionRepository.findUserIdsByAlmanaxId(todaysAlmanax.getId());
        if (userIds.isEmpty()) {
            log.info("No subscriptions for almanax '{}' (id={}), skipping", todaysAlmanax.getName(), todaysAlmanax.getId());
            return;
        }

        log.info("Sending almanax notification for '{}' to {} user(s)", todaysAlmanax.getName(), userIds.size());

        String title = "Almanax - " + todaysAlmanax.getName();
        String body = todaysAlmanax.getDescription();

        for (Long userId : userIds) {
            try {
                eventPublisher.publishEvent(NotificationEvent.user(userId, title, body, NotificationType.INFO));
            } catch (Exception e) {
                log.error("Failed to send almanax notification to user {}: {}", userId, e.getMessage());
            }
        }
    }

    private Almanax findBestAlmanaxForDate(LocalDate date, List<Almanax> almanaxList) {
        Almanax best = null;
        int bestScore = 0;
        int bestPatternCount = Integer.MAX_VALUE;

        for (Almanax almanax : almanaxList) {
            int score = almanax.getDates().stream()
                    .mapToInt(raw -> new DatePattern(raw).score(date))
                    .max()
                    .orElse(0);

            if (score == 0) continue;

            int patternCount = almanax.getDates().size();
            if (score > bestScore || (score == bestScore && patternCount < bestPatternCount)) {
                bestScore = score;
                bestPatternCount = patternCount;
                best = almanax;
            }
        }

        return best;
    }
}