package fr.huiitre.tools.config.core.role;

import javax.sql.DataSource;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.core.role.application.ports.RoleRepository;
import fr.huiitre.tools.modules.core.role.infrastructure.PostgresRoleRepository;

@Configuration
public class RoleConfig {

    @Bean
    public RoleRepository roleRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresRoleRepository(jdbcTemplate);
    }

}
