package fr.huiitre.tools.modules.core.auth.infrastructure.scheduler;

import java.time.LocalDateTime;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.auth.application.ports.UserEmailVerificationRepository;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;

@Component
public class EmailVerificationCleanupScheduler {

    private final UserRepository userRepository;

    private final UserEmailVerificationRepository userEmailVerificationRepository;

    private static final Logger logger = LoggerFactory.getLogger(EmailVerificationCleanupScheduler.class);

    public EmailVerificationCleanupScheduler(
            UserEmailVerificationRepository userEmailVerificationRepository, UserRepository userRepository) {
        this.userEmailVerificationRepository = userEmailVerificationRepository;
        this.userRepository = userRepository;
    }

    /**
     * Nettoie les tokens de validation email expirés.
     * Exécution toutes les 30 minutes.
     */
    @Scheduled(cron = "0 */30 * * * *")
    // @Scheduled(cron = "*/10 * * * * *")
    @Transactional
    public void cleanupExpiredTokens() {

        LocalDateTime now = LocalDateTime.now();

        logger.info("EMAIL_VERIFICATION_CLEANUP_START");
        userRepository.deleteUnvalidatedUsersWithExpiredEmailVerification(now);
        userRepository.deleteUnvalidatedUsersWithoutEmailVerification();
        userEmailVerificationRepository.deleteExpired(now);
        logger.info("EMAIL_VERIFICATION_CLEANUP_END");
    }
}
