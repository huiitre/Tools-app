package fr.huiitre.tools.config.palworld;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.workpriority.application.ports.WorkPriorityRepository;
import fr.huiitre.tools.modules.palworld.workpriority.infrastructure.PostgresWorkPriorityRepository;

@Configuration
public class PalworldWorkPriorityConfig {

    @Bean
    public WorkPriorityRepository workPriorityRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresWorkPriorityRepository(jdbcTemplate);
    }
}
