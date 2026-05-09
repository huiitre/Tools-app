package fr.huiitre.tools.config.core.module;

import javax.sql.DataSource;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.module.application.ports.ModuleRepository;
import fr.huiitre.tools.modules.core.module.infrastructure.PostgresModuleRepository;

@Configuration
public class ModuleConfig {

    @Bean
    public ModuleRepository moduleRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresModuleRepository(jdbcTemplate);
    }
}
