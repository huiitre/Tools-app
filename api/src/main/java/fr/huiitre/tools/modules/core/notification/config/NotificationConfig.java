package fr.huiitre.tools.modules.core.notification.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.notification.application.port.NotificationRepository;
import fr.huiitre.tools.modules.core.notification.infrastructure.persistence.PostgresNotificationRepository;

@Configuration
public class NotificationConfig {

    @Bean
    public NotificationRepository notificationRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresNotificationRepository(jdbcTemplate);
    }
}
