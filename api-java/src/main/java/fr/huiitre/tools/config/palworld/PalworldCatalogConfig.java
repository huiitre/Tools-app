package fr.huiitre.tools.config.palworld;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.catalog.application.ports.ItemCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.ports.PalCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.ports.PassiveSkillCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.application.ports.ShopCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.infrastructure.PostgresItemCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.infrastructure.PostgresPalCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.infrastructure.PostgresPassiveSkillCatalogRepository;
import fr.huiitre.tools.modules.palworld.catalog.infrastructure.PostgresShopCatalogRepository;

@Configuration
public class PalworldCatalogConfig {

    @Bean
    public PalCatalogRepository palCatalogRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalCatalogRepository(jdbcTemplate);
    }

    @Bean
    public PassiveSkillCatalogRepository passiveSkillCatalogRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPassiveSkillCatalogRepository(jdbcTemplate);
    }

    @Bean
    public ShopCatalogRepository shopCatalogRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresShopCatalogRepository(jdbcTemplate);
    }

    @Bean
    public ItemCatalogRepository itemCatalogRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresItemCatalogRepository(jdbcTemplate);
    }
}
