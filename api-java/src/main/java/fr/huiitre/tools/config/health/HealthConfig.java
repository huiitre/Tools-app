package fr.huiitre.tools.config.health;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.health.weight_log.application.ports.WeightLogRepository;
import fr.huiitre.tools.modules.health.weight_log.infrastructure.PostgresWeightLogRepository;

@Configuration
public class HealthConfig {

    @Bean
    public WeightLogRepository weightLogRepository(
            JdbcTemplate jdbcTemplate) {
        return new PostgresWeightLogRepository(
                jdbcTemplate);
    }
}
