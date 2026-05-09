package fr.huiitre.tools.config.core.admin;

import fr.huiitre.tools.modules.core.admin.application.ports.AdminStatsRepository;
import fr.huiitre.tools.modules.core.admin.infrastructure.PostgresAdminStatsRepository;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

@Configuration
public class AdminConfig {

    @Bean
    public AdminStatsRepository adminStatsRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresAdminStatsRepository(jdbcTemplate);
    }
}
