package fr.huiitre.tools.modules.core.auth.infrastructure.scheduler;

import java.time.LocalDateTime;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import fr.huiitre.tools.modules.core.auth.application.ports.UserPasswordResetRepository;

@Component
public class PasswordResetRequestCleanupScheduler {

    private final UserPasswordResetRepository userPasswordResetRepository;
    private static final Logger logger = LoggerFactory.getLogger(PasswordResetRequestCleanupScheduler.class);

    public PasswordResetRequestCleanupScheduler(
            UserPasswordResetRepository userPasswordResetRepository) {
        this.userPasswordResetRepository = userPasswordResetRepository;
    }

    @Scheduled(cron = "0 */30 * * * *")
    public void cleanupExpiredPasswordResetRequests() {

        logger.info("PASSWORD_RESET_REQUEST_CLEANUP_START");
        userPasswordResetRepository.deleteExpired(LocalDateTime.now());
        logger.info("PASSWORD_RESET_REQUEST_CLEANUP_END");
    }
}
