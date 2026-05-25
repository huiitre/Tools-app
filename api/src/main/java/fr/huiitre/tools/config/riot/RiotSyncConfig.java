package fr.huiitre.tools.config.riot;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantBundleDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantBundleSyncRepository;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantSkinChromaSyncRepository;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantSkinDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantSkinLevelSyncRepository;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantSkinSyncRepository;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantWeaponDataProvider;
import fr.huiitre.tools.modules.riot.sync.application.ports.ValorantWeaponSyncRepository;
import fr.huiitre.tools.modules.riot.sync.infrastructure.PostgresValorantBundleSyncRepository;
import fr.huiitre.tools.modules.riot.sync.infrastructure.PostgresValorantSkinChromaSyncRepository;
import fr.huiitre.tools.modules.riot.sync.infrastructure.PostgresValorantSkinLevelSyncRepository;
import fr.huiitre.tools.modules.riot.sync.infrastructure.PostgresValorantSkinSyncRepository;
import fr.huiitre.tools.modules.riot.sync.infrastructure.PostgresValorantWeaponSyncRepository;
import fr.huiitre.tools.modules.riot.sync.infrastructure.ValorantLocalAssetsReader;
import fr.huiitre.tools.modules.riot.sync.infrastructure.ValorantLocalBundleDataProvider;
import fr.huiitre.tools.modules.riot.sync.infrastructure.ValorantLocalSkinDataProvider;
import fr.huiitre.tools.modules.riot.sync.infrastructure.ValorantLocalWeaponDataProvider;

@Configuration
public class RiotSyncConfig {

    @Bean
    public ValorantWeaponDataProvider valorantWeaponDataProvider(
            ValorantLocalAssetsReader assetsReader,
            @Value("${app.assets.base-url}") String assetsBaseUrl) {
        return new ValorantLocalWeaponDataProvider(assetsReader, assetsBaseUrl);
    }

    @Bean
    public ValorantWeaponSyncRepository valorantWeaponSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantWeaponSyncRepository(jdbcTemplate);
    }

    @Bean
    public ValorantSkinDataProvider valorantSkinDataProvider(
            ValorantLocalAssetsReader assetsReader,
            @Value("${app.assets.base-url}") String assetsBaseUrl) {
        return new ValorantLocalSkinDataProvider(assetsReader, assetsBaseUrl);
    }

    @Bean
    public ValorantSkinSyncRepository valorantSkinSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantSkinSyncRepository(jdbcTemplate);
    }

    @Bean
    public ValorantBundleDataProvider valorantBundleDataProvider(
            ValorantLocalAssetsReader assetsReader,
            @Value("${app.assets.base-url}") String assetsBaseUrl) {
        return new ValorantLocalBundleDataProvider(assetsReader, assetsBaseUrl);
    }

    @Bean
    public ValorantSkinLevelSyncRepository valorantSkinLevelSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantSkinLevelSyncRepository(jdbcTemplate);
    }

    @Bean
    public ValorantSkinChromaSyncRepository valorantSkinChromaSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantSkinChromaSyncRepository(jdbcTemplate);
    }

    @Bean
    public ValorantBundleSyncRepository valorantBundleSyncRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresValorantBundleSyncRepository(jdbcTemplate);
    }
}
