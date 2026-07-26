package fr.huiitre.tools.config.palworld;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.catalog.application.ports.PalCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.infrastructure.PostgresPalCatalogRepository;

@Configuration
public class PalworldCatalogConfig {

    @Bean
    public PalCatalogRepository palCatalogRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalCatalogRepository(jdbcTemplate);
    }
}
