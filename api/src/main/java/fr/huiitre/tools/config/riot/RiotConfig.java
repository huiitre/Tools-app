package fr.huiitre.tools.config.riot;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.web.client.RestTemplate;

import fr.huiitre.tools.modules.riot.sync.infrastructure.ValorantLocalAssetsReader;
import fr.huiitre.tools.modules.riot.valorant.application.ports.RiotAuthPort;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantVersionProvider;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.application.ports.ValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantBundleRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantUserSkinRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantWatchlistRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.PostgresValorantWeaponRepository;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.RiotAuthHttpAdapter;
import fr.huiitre.tools.modules.riot.valorant.infrastructure.ValorantLocalVersionProvider;

@Configuration
public class RiotConfig {

    @Bean
    public RiotAuthPort riotAuthPort() {
        return new RiotAuthHttpAdapter(new RestTemplate());
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
}
