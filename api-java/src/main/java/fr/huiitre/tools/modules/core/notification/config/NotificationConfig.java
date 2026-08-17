package fr.huiitre.tools.modules.core.notification.config;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.client.RestTemplate;

import fr.huiitre.tools.modules.core.notification.application.port.ApiCoreNotificationPort;
import fr.huiitre.tools.modules.core.notification.infrastructure.http.ApiCoreNotificationHttpAdapter;

@Configuration
public class NotificationConfig {

    @Bean
    public ApiCoreNotificationPort apiCoreNotificationPort(
            @Value("${tools.core.base-url}") String baseUrl,
            @Value("${tools.core.internal-token}") String internalToken) {
        return new ApiCoreNotificationHttpAdapter(new RestTemplate(), baseUrl, internalToken);
    }
}
