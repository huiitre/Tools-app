package fr.huiitre.tools.config.palworld;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

import fr.huiitre.tools.modules.palworld.sync.infrastructure.PalworldLocalAssetsReader;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalLookupRepository;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.PalTierRankingRepository;
import fr.huiitre.tools.modules.palworld.tierlist.application.ports.TierListDataProvider;
import fr.huiitre.tools.modules.palworld.tierlist.infrastructure.PalworldLocalTierListDataProvider;
import fr.huiitre.tools.modules.palworld.tierlist.infrastructure.PostgresPalLookupRepository;
import fr.huiitre.tools.modules.palworld.tierlist.infrastructure.PostgresPalTierRankingRepository;

@Configuration
public class PalworldTierListConfig {

    @Bean
    public TierListDataProvider tierListDataProvider(PalworldLocalAssetsReader assetsReader) {
        return new PalworldLocalTierListDataProvider(assetsReader);
    }

    @Bean
    public PalLookupRepository tierListPalLookupRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalLookupRepository(jdbcTemplate);
    }

    @Bean
    public PalTierRankingRepository palTierRankingRepository(JdbcTemplate jdbcTemplate) {
        return new PostgresPalTierRankingRepository(jdbcTemplate);
    }
}
