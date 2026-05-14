package fr.huiitre.tools.modules.riot.valorant.infrastructure.scheduler;

import fr.huiitre.tools.modules.riot.valorant.application.user.usecase.ValorantWatchlistNotifier;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

@Component
public class ValorantWatchlistScheduler {

    private static final Logger log = LoggerFactory.getLogger(ValorantWatchlistScheduler.class);
    private final ValorantWatchlistNotifier watchlistNotifier;

    public ValorantWatchlistScheduler(ValorantWatchlistNotifier watchlistNotifier) {
        this.watchlistNotifier = watchlistNotifier;
    }

    @EventListener(ApplicationReadyEvent.class)
    public void onStart() {
        log.info("Server started, checking if Valorant sync is needed...");
        runDailyValorantSync();
    }

    // Exécution quotidienne à 6h00 du matin
    @Scheduled(cron = "0 0 6 * * *")
    public void runDailyValorantSync() {
        log.info("Executing daily Valorant Watchlist & History sync...");
        watchlistNotifier.processAllUsers();
        log.info("Daily Valorant sync completed.");
    }
}
