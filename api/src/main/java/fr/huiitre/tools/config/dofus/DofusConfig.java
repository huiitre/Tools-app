package fr.huiitre.tools.config.dofus;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxRepository;
import fr.huiitre.tools.modules.dofus.almanax.application.ports.AlmanaxSubscriptionRepository;
import fr.huiitre.tools.modules.dofus.almanax.infrastructure.PostgresAlmanaxRepository;
import fr.huiitre.tools.modules.dofus.almanax.infrastructure.PostgresAlmanaxSubscriptionRepository;
import fr.huiitre.tools.modules.dofus.area.application.ports.AreaRepository;
import fr.huiitre.tools.modules.dofus.area.infrastructure.PostgresAreaRepository;
import fr.huiitre.tools.modules.dofus.catalogue.application.ports.CatalogueItemRepository;
import fr.huiitre.tools.modules.dofus.catalogue.infrastructure.PostgresCatalogueItemRepository;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameServerRepository;
import fr.huiitre.tools.modules.dofus.game.application.ports.GameVersionRepository;
import fr.huiitre.tools.modules.dofus.game.infrastructure.PostgresGameServerRepository;
import fr.huiitre.tools.modules.dofus.game.infrastructure.PostgresGameVersionRepository;
import fr.huiitre.tools.modules.dofus.item.application.ports.ItemRepository;
import fr.huiitre.tools.modules.dofus.item.infrastructure.PostgresItemRepository;
import fr.huiitre.tools.modules.dofus.itemtype.application.ports.ItemTypeRepository;
import fr.huiitre.tools.modules.dofus.itemtype.infrastructure.PostgresItemTypeRepository;
import fr.huiitre.tools.modules.dofus.monster.application.ports.MonsterRepository;
import fr.huiitre.tools.modules.dofus.monster.domain.Monster;
import fr.huiitre.tools.modules.dofus.monster.infrastructure.PostgresMonsterRepository;
import fr.huiitre.tools.modules.dofus.pricing.application.ports.ItemPriceRepository;
import fr.huiitre.tools.modules.dofus.pricing.infrastructure.PostgresItemPriceRepository;
import fr.huiitre.tools.modules.dofus.recipe.application.ports.RecipeRepository;
import fr.huiitre.tools.modules.dofus.recipe.infrastructure.PostgresRecipeRepository;
import fr.huiitre.tools.modules.dofus.subarea.application.ports.SubareaRepository;
import fr.huiitre.tools.modules.dofus.subarea.infrastructure.PostgresSubareaRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.application.repository.WorkshopTagRepository;
import fr.huiitre.tools.modules.dofus.workshop.domain.WorkshopTag;
import fr.huiitre.tools.modules.dofus.workshop.infrastructure.PostgresWorkshopRepository;
import fr.huiitre.tools.modules.dofus.workshop.infrastructure.PostgresWorkshopTagRepository;

@Configuration
public class DofusConfig {

        @Bean
        public GameVersionRepository gameVersionRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresGameVersionRepository(jdbcTemplate);
        }

        @Bean
        public GameServerRepository gameServerRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresGameServerRepository(
                                jdbcTemplate);
        }

        @Bean
        public ItemTypeRepository itemTypeRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresItemTypeRepository(
                                jdbcTemplate);
        }

        @Bean
        public ItemRepository itemRepository(
                        JdbcTemplate jdbcTemplate,
                        NamedParameterJdbcTemplate namedParameterJdbcTemplate) {
                return new PostgresItemRepository(
                                jdbcTemplate, namedParameterJdbcTemplate);
        }

        @Bean
        public AlmanaxRepository almanaxRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresAlmanaxRepository(
                                jdbcTemplate);
        }

        @Bean
        public AlmanaxSubscriptionRepository almanaxSubscriptionRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresAlmanaxSubscriptionRepository(
                                jdbcTemplate);
        }

        @Bean
        public ItemPriceRepository itemPriceRepository(
                        NamedParameterJdbcTemplate jdbcTemplate) {
                return new PostgresItemPriceRepository(
                                jdbcTemplate);
        }

        @Bean
        public CatalogueItemRepository catalogueItemRepository(
                        NamedParameterJdbcTemplate jdbcTemplate) {
                return new PostgresCatalogueItemRepository(
                                jdbcTemplate);
        }

        @Bean
        public RecipeRepository recipeRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresRecipeRepository(
                                jdbcTemplate);
        }

        @Bean
        public AreaRepository areaRepository(
                JdbcTemplate jdbcTemplate) {
                return new PostgresAreaRepository(
                jdbcTemplate);
        }

        @Bean
        public SubareaRepository subareaRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresSubareaRepository(
                        jdbcTemplate);
        }

        @Bean
        public MonsterRepository monsterRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresMonsterRepository(
                        jdbcTemplate,
                new NamedParameterJdbcTemplate(jdbcTemplate));
        }

        @Bean
        public WorkshopTagRepository workshopTagRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresWorkshopTagRepository(
                        jdbcTemplate);
        }

        @Bean
        public WorkshopRepository workshopRepository(
                        JdbcTemplate jdbcTemplate) {
                return new PostgresWorkshopRepository(
                        jdbcTemplate);
        }
}
