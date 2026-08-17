package fr.huiitre.tools.modules.palworld.serverdata.infrastructure;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import fr.huiitre.tools.modules.palworld.serverdata.application.ServerDataSyncReport;
import fr.huiitre.tools.modules.palworld.serverdata.application.usecase.SyncServerDataUseCase;

@Component
@ConditionalOnProperty(prefix = "palworld.server-data", name = "enabled", havingValue = "true", matchIfMissing = true)
public class PalworldServerDataScheduler {

    private static final Logger log = LoggerFactory.getLogger(PalworldServerDataScheduler.class);

    private final SyncServerDataUseCase syncServerDataUseCase;

    public PalworldServerDataScheduler(SyncServerDataUseCase syncServerDataUseCase) {
        this.syncServerDataUseCase = syncServerDataUseCase;
    }

    // fixedDelay (contrairement à cron) exécute déjà une première fois dès le démarrage du contexte,
    // pas besoin d'un @EventListener(ApplicationReadyEvent) en plus comme pour le scheduler Valorant (cron).
    @Scheduled(fixedDelayString = "${palworld.server-data.scheduler.fixed-delay-ms:300000}")
    public void run() {
        log.info("Checking for Palworld server-data snapshot files to import...");
        ServerDataSyncReport report = syncServerDataUseCase.execute();
        log.info("Palworld server-data sync done: {} imported, {} already imported (moved only), {} failed",
                report.filesImported(), report.filesAlreadyImportedButNotMoved(), report.filesFailed());
    }
}
