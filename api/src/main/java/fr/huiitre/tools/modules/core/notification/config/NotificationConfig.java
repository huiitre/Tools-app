package fr.huiitre.tools.modules.core.notification.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.web.client.RestTemplate;

import fr.huiitre.tools.modules.core.notification.application.port.ApiCoreNotificationPort;
import fr.huiitre.tools.modules.core.notification.application.port.NotificationRepository;
import fr.huiitre.tools.modules.core.notification.infrastructure.http.ApiCoreNotificationHttpAdapter;
import fr.huiitre.tools.modules.core.notification.infrastructure.persistence.PostgresNotificationRepository;

@Configuration
public class NotificationConfig {

    @Bean
    public NotificationRepository notificationRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresNotificationRepository(jdbcTemplate);
    }

    @Bean
    public ApiCoreNotificationPort apiCoreNotificationPort(
            @Value("${tools.core.base-url}") String baseUrl,
            @Value("${tools.core.internal-token}") String internalToken) {
        return new ApiCoreNotificationHttpAdapter(new RestTemplate(), baseUrl, internalToken);
    }
}
