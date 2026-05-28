package fr.huiitre.tools.modules.dofus.almanax.infrastructure.scheduler;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import fr.huiitre.tools.modules.dofus.almanax.application.usecase.AlmanaxSubscriptionNotifier;

@Component
public class AlmanaxSubscriptionScheduler {

    private static final Logger log = LoggerFactory.getLogger(AlmanaxSubscriptionScheduler.class);

    private final AlmanaxSubscriptionNotifier notifier;

    public AlmanaxSubscriptionScheduler(AlmanaxSubscriptionNotifier notifier) {
        this.notifier = notifier;
    }

    @Scheduled(cron = "0 0 6 * * *")
    public void runDailyAlmanaxNotifications() {
        log.info("Running daily Almanax subscription notifications...");
        notifier.processToday();
        log.info("Daily Almanax notifications completed.");
    }
}