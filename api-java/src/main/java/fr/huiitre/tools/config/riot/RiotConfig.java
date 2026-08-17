package fr.huiitre.tools.config.riot;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.web.client.RestTemplate;

import fr.huiitre.tools.modules.riot.sync.infrastructure.ValorantLocalAssetsReader;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantVersionProvider;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.application.user.ports.ValorantStoreHistoryRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantStoreHistoryRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.RiotAuthHttpAdapter;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.ValorantLocalVersionProvider;

import com.fasterxml.jackson.databind.ObjectMapper;
import fr.huiitre.tools.modules.riot.valorant.application.core.ports.ValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantAuthRepository;
import fr.huiitre.tools.modules.riot.valorant.application.catalog.ports.ValorantStorePort;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.ValorantStoreHttpAdapter;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.ValorantTokenParser;

@Configuration
public class RiotConfig {

    @Bean
    public RiotAuthPort riotAuthPort(ObjectMapper objectMapper, ValorantTokenParser tokenParser) {
        return new RiotAuthHttpAdapter(new RestTemplate(), tokenParser);
    }

    @Bean
    public ValorantStorePort valorantStorePort() {
        return new ValorantStoreHttpAdapter(new RestTemplate());
    }

    @Bean
    public ValorantAuthRepository valorantAuthRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantAuthRepository(jdbcTemplate);
    }

    @Bean
    public ValorantSkinRepository valorantSkinRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantSkinRepository(jdbcTemplate);
    }

    @Bean
    public ValorantBundleRepository valorantBundleRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantBundleRepository(jdbcTemplate);
    }

    @Bean
    public ValorantVersionProvider valorantVersionProvider(ValorantLocalAssetsReader assetsReader) {
        return new ValorantLocalVersionProvider(assetsReader);
    }

    @Bean
    public ValorantWeaponRepository valorantWeaponRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantWeaponRepository(jdbcTemplate);
    }

    @Bean
    public ValorantWatchlistRepository valorantWatchlistRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantWatchlistRepository(jdbcTemplate);
    }

    @Bean
    public ValorantUserSkinRepository valorantUserSkinRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantUserSkinRepository(jdbcTemplate);
    }

    @Bean
    public ValorantStoreHistoryRepository valorantStoreHistoryRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantStoreHistoryRepository(jdbcTemplate);
    }
}

