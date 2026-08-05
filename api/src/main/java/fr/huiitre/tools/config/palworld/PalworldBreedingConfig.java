package fr.huiitre.tools.config.palworld;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.breeding.application.ports.BreedingCatalogRepository;
import fr.huiitre.tools.modules.palworld.breeding.infrastructure.PostgresBreedingCatalogRepository;

@Configuration
public class PalworldBreedingConfig {

    @Bean
    public BreedingCatalogRepository breedingCatalogRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresBreedingCatalogRepository(jdbcTemplate);
    }
}
